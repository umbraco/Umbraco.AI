using System.Text.Json;
using Json.Schema;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Helpers;
using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core.Services;
using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// An Automate action that executes an AI agent with a given message and returns the response.
/// Uses dynamic output schema resolved from the agent's configured output schema.
/// Runs the agent as the automation workspace's service account.
/// </summary>
[Action(UmbracoAIAutomateConstants.ActionTypes.RunAgent, "Run AI Agent",
    Description = "Executes an AI agent and returns its response.",
    Group = "AI",
    Icon = "icon-bot")]
public sealed class RunAgentAction : ActionBase<RunAgentSettings, object>
{
    private readonly IAIAgentService _agentService;
    private readonly IUserService _userService;
    private readonly ILogger<RunAgentAction> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAgentAction"/> class.
    /// </summary>
    public RunAgentAction(
        ActionInfrastructure infrastructure,
        IAIAgentService agentService,
        IUserService userService,
        ILogger<RunAgentAction> logger)
        : base(infrastructure)
    {
        _agentService = agentService;
        _userService = userService;
        _logger = logger;
    }

    /// <inheritdoc />
    public override bool HasDynamicOutputSchema => true;

    /// <inheritdoc />
    protected override async Task<JsonSchema?> GetOutputSchemaAsync(
        RunAgentSettings? settings,
        CancellationToken cancellationToken = default)
    {
        if (settings is null || settings.AgentId == Guid.Empty)
        {
            return null;
        }

        return await AgentOutputSchemaHelper.GetOutputSchemaAsync(_agentService, settings.AgentId, cancellationToken);
    }

    /// <summary>
    /// The maximum allowed nesting depth for agent-triggered automations.
    /// Prevents infinite recursion when an agent triggers an automation that runs another agent.
    /// </summary>
    public const int MaxAgentNestingDepth = 3;

    /// <summary>
    /// The key used in trigger output data to track agent nesting depth.
    /// </summary>
    public const string AgentNestingDepthKey = "_aiAgentDepth";

    /// <inheritdoc />
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<RunAgentSettings>();

        if (settings.AgentId == Guid.Empty)
        {
            return ActionResult.Failed(
                new ArgumentException("Agent is required."),
                StepRunErrorCategory.Validation);
        }

        // Recursion guard: check if this automation was triggered by an AI agent tool.
        // If so, the trigger output data contains a nesting depth counter.
        var currentDepth = GetAgentNestingDepth(context);
        if (currentDepth >= MaxAgentNestingDepth)
        {
            _logger.LogWarning(
                "Automation {AutomationId} / Run {RunId}: Refusing to execute AI agent {AgentId} — " +
                "agent nesting depth {Depth} exceeds maximum {MaxDepth}",
                context.AutomationId, context.RunId, settings.AgentId, currentDepth, MaxAgentNestingDepth);

            return ActionResult.Failed(
                new InvalidOperationException(
                    $"Agent nesting depth {currentDepth} exceeds maximum {MaxAgentNestingDepth}. " +
                    "This prevents infinite recursion when agents trigger automations that run agents."),
                StepRunErrorCategory.Validation);
        }

        _logger.LogInformation(
            "Automation {AutomationId} / Run {RunId}: Executing AI agent {AgentId}",
            context.AutomationId, context.RunId, settings.AgentId);

        try
        {
            AIAgent? agent = await _agentService.GetAgentAsync(settings.AgentId, cancellationToken);
            if (agent is null)
            {
                return ActionResult.Failed(
                    new InvalidOperationException($"Agent '{settings.AgentId}' not found."),
                    StepRunErrorCategory.Validation);
            }

            // Resolve the service account's user groups for agent tool permission resolution.
            // In headless/automation context there is no backoffice user, so we must pass
            // the workspace service account's groups explicitly via AIAgentExecutionOptions.
            var userGroupIds = await ResolveServiceAccountGroupIdsAsync(context);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, settings.Message),
            };

            var options = new AIAgentExecutionOptions
            {
                UserGroupIds = userGroupIds,
            };

            AgentResponse response = await _agentService.RunAgentAsync(
                agent.Id,
                messages,
                options,
                cancellationToken);

            var responseText = response.Text;

            _logger.LogDebug(
                "Automation {AutomationId} / Run {RunId}: Agent {AgentId} responded with {MessageCount} message(s), text length {TextLength}",
                context.AutomationId, context.RunId, settings.AgentId, response.Messages.Count, responseText.Length);

            var outputData = TryParseStructuredOutput(responseText);

            return Success(outputData);
        }
        catch (InvalidOperationException ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.Validation);
        }
        catch (OperationCanceledException ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Automation {AutomationId} / Run {RunId}: AI agent {AgentId} execution failed",
                context.AutomationId, context.RunId, settings.AgentId);
            return ActionResult.Failed(ex, StepRunErrorCategory.Unknown);
        }
    }

    private static object TryParseStructuredOutput(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return new { response = string.Empty };
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(responseText);
            if (parsed is not null)
            {
                return parsed;
            }
        }
        catch (JsonException)
        {
            // Not valid JSON -- fall through to plain text response
        }

        return new { response = responseText };
    }

    private async Task<IEnumerable<Guid>?> ResolveServiceAccountGroupIdsAsync(ActionContext context)
    {
        var serviceAccountKey = context.ExecutionContext?.ServiceAccountKey;
        if (serviceAccountKey is null)
        {
            return null;
        }

        var user = await _userService.GetAsync(serviceAccountKey.Value);
        return user?.Groups.Select(g => g.Key).ToList();
    }

    /// <summary>
    /// Reads the agent nesting depth from the automation's binding data.
    /// The depth is set by the <c>run_automation</c> AI tool when it triggers an automation.
    /// </summary>
    private static int GetAgentNestingDepth(ActionContext context)
    {
        if (context.BindingData is null)
        {
            return 0;
        }

        // The trigger output data is stored under the "trigger" key in binding data.
        // Check for our depth marker in the trigger's output.
        if (context.BindingData.TryGetValue("trigger", out var triggerData)
            && triggerData is IDictionary<string, object?> triggerDict
            && triggerDict.TryGetValue(AgentNestingDepthKey, out var depthValue))
        {
            return depthValue switch
            {
                int i => i,
                long l => (int)l,
                JsonElement { ValueKind: JsonValueKind.Number } je => je.GetInt32(),
                _ => 0,
            };
        }

        // Also check the top-level binding data in case the structure differs.
        if (context.BindingData.TryGetValue(AgentNestingDepthKey, out var topLevelDepth))
        {
            return topLevelDepth switch
            {
                int i => i,
                long l => (int)l,
                JsonElement { ValueKind: JsonValueKind.Number } je => je.GetInt32(),
                _ => 0,
            };
        }

        return 0;
    }
}

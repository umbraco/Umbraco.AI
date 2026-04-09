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

            var outputData = TryParseStructuredOutput(response.Text);

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
}

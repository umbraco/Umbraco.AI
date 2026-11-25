using System.Text.Json;

using Json.Schema;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Umbraco.AI.Agent.Core;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Helpers;
using Umbraco.AI.Automate.Security;
using Umbraco.AI.Automate.Triggers;
using Umbraco.AI.Core.Media;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Security;
using Umbraco.Automate.Core.StepTypes;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;

using CoreConstants = Umbraco.AI.Core.Constants;

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
public sealed class RunAgentAction : ActionBase<RunAgentSettings, object>, IPublishValidatableStepType
{
    private readonly IAIAgentService _agentService;
    private readonly IUserService _userService;
    private readonly IMediaService _mediaService;
    private readonly IAIUmbracoMediaResolver _mediaResolver;
    private readonly IAutomationActionAuthorizer _authorizer;
    private readonly AgentAccessChecker _accessChecker;
    private readonly IAIAgentUnrestrictedReader _unrestrictedReader;
    private readonly ILogger<RunAgentAction> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAgentAction"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IMediaService, IAIUmbracoMediaResolver and IAutomationActionAuthorizer so media can be attached. Will be removed in v20.")]
    public RunAgentAction(
        ActionInfrastructure infrastructure,
        IAIAgentService agentService,
        IUserService userService,
        ILogger<RunAgentAction> logger)
        : this(
            infrastructure,
            agentService,
            userService,
            StaticServiceProvider.Instance.GetRequiredService<IMediaService>(),
            StaticServiceProvider.Instance.GetRequiredService<IAIUmbracoMediaResolver>(),
            StaticServiceProvider.Instance.GetRequiredService<IAutomationActionAuthorizer>(),
            logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAgentAction"/> class.
    /// </summary>
    /// <remarks>
    /// Marked as the activation constructor so <c>ActivatorUtilities</c> doesn't find the
    /// obsolete overload ambiguous.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public RunAgentAction(
        ActionInfrastructure infrastructure,
        IAIAgentService agentService,
        IUserService userService,
        IMediaService mediaService,
        IAIUmbracoMediaResolver mediaResolver,
        IAutomationActionAuthorizer authorizer,
        ILogger<RunAgentAction> logger)
        : this(
            infrastructure,
            agentService,
            userService,
            mediaService,
            mediaResolver,
            authorizer,
            StaticServiceProvider.Instance.GetRequiredService<AgentAccessChecker>(),
            StaticServiceProvider.Instance.GetRequiredService<IAIAgentUnrestrictedReader>(),
            logger)
    {
    }

    // The checker is internal, so it can't be a parameter of a public constructor; this one is for tests.
    internal RunAgentAction(
        ActionInfrastructure infrastructure,
        IAIAgentService agentService,
        IUserService userService,
        IMediaService mediaService,
        IAIUmbracoMediaResolver mediaResolver,
        IAutomationActionAuthorizer authorizer,
        AgentAccessChecker accessChecker,
        IAIAgentUnrestrictedReader unrestrictedReader,
        ILogger<RunAgentAction> logger)
        : base(infrastructure)
    {
        _accessChecker = accessChecker;
        _unrestrictedReader = unrestrictedReader;
        _agentService = agentService;
        _userService = userService;
        _mediaService = mediaService;
        _mediaResolver = mediaResolver;
        _authorizer = authorizer;
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

        return await AgentOutputSchemaHelper.GetOutputSchemaAsync(_unrestrictedReader, settings.AgentId, cancellationToken);
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

    /// <summary>
    /// The maximum number of media items that can be attached to a single agent run.
    /// </summary>
    public const int MaxAttachments = 10;

    /// <summary>
    /// The maximum combined size, in bytes, of the media attached to a single agent run.
    /// Images are already downscaled by the media resolver; this bounds everything else
    /// (documents, audio) so a single run can't send an unbounded request to the provider.
    /// </summary>
    public const long MaxTotalAttachmentBytes = 20 * 1024 * 1024;

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

        AgentAccess? access = null;

        try
        {
            // The service account's groups, not the ambient backoffice user's, decide whether the agent may run:
            // an automation run has no backoffice user, or the wrong one. No service account means no groups, so
            // a limited agent is refused and never runs. Automate's BackOfficeIdentityMiddleware already fails the
            // step with Authentication when the workspace has no service account, so this branch is a second safety
            // net (R19: an agent with a limit is never run without a known user).
            var serviceAccount = await ResolveServiceAccountAsync(context);
            access = await _accessChecker.CheckAsync(settings.AgentId, serviceAccount, cancellationToken);

            if (access.Status == AgentAccessStatus.NotFound)
            {
                return ActionResult.Failed(
                    new InvalidOperationException($"Agent '{settings.AgentId}' not found."),
                    StepRunErrorCategory.Validation);
            }

            if (access.Status == AgentAccessStatus.Refused)
            {
                return RefusedFailure(access.RefusalReason);
            }

            var userGroupIds = serviceAccount?.Groups.Select(g => g.Key).ToList() ?? [];

            var (attachments, attachmentFailure) = await ResolveAttachmentsAsync(settings.Attachments, cancellationToken);
            if (attachmentFailure is not null)
            {
                return attachmentFailure;
            }

            var contents = new List<AIContent> { new TextContent(settings.Message) };
            contents.AddRange(attachments);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, contents),
            };

            // Populate metadata context keys so AIAuditingChatClient can persist RunId/ThreadId
            // onto the resulting AIAuditLog.Metadata column. Mirrors the AG-UI streaming path.
            // TryAdd preserves caller-supplied values if this surface ever accepts pre-populated entries.
            var additionalProperties = new Dictionary<string, object?>();
            additionalProperties.TryAdd(Constants.ContextKeys.RunId, Guid.NewGuid().ToString());
            additionalProperties.TryAdd(Constants.ContextKeys.ThreadId, context.RunId.ToString());
            additionalProperties.TryAdd(
                CoreConstants.ContextKeys.LogKeys,
                new[] { Constants.ContextKeys.RunId, Constants.ContextKeys.ThreadId });

            var options = new AIAgentExecutionOptions
            {
                UserGroupIds = userGroupIds,
                AdditionalProperties = additionalProperties,
                ApprovalPolicy = RunAgentToolPermissionsExtensions.ToApprovalPolicy(settings.ToolPermissions),
            };

            // Mark the async flow as Automate-driven so the agent run triggers
            // (AgentRunCompletedTrigger / AgentRunFailedTrigger) suppress themselves
            // for this run and we don't recurse into an unbounded loop when an
            // automation listens to the same trigger that its RunAgent step emits.
            using var _ = AutomateAgentRunScope.Enter();

            AgentResponse response = await _agentService.RunAgentAsync(
                settings.AgentId,
                messages,
                options,
                cancellationToken);

            var responseText = response.Text;

            _logger.LogDebug(
                "Automation {AutomationId} / Run {RunId}: Agent {AgentId} responded with {MessageCount} message(s), text length {TextLength}",
                context.AutomationId, context.RunId, settings.AgentId, response.Messages.Count, responseText.Length);

            var outputData = BuildOutputData(responseText);

            return Success(outputData);
        }
        catch (AIAgentNotAvailableException) when (access is not null)
        {
            // Access was checked up front; this is the agent service refusing too, if the limit changed since.
            return RefusedFailure(access.RefusalReason);
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
            return ActionResult.Failed(ex, StepErrorCategoryMapping.FromException(ex));
        }
    }

    /// <summary>
    /// The reserved output property that always holds the agent's raw response text,
    /// regardless of whether the agent uses a structured output schema. This guarantees
    /// a bindable property is always available to downstream automation steps.
    /// </summary>
    public const string RawResponseKey = "response";

    /// <summary>
    /// Builds the action output data from the agent's raw response text.
    /// The raw text is always exposed under the reserved <see cref="RawResponseKey"/> property.
    /// If the response is a JSON object (structured output), its properties are merged in
    /// alongside the raw response so both forms are bindable. The reserved raw response key
    /// is never overwritten by a structured property of the same name.
    /// </summary>
    private static Dictionary<string, object?> BuildOutputData(string responseText)
    {
        var output = new Dictionary<string, object?>
        {
            [RawResponseKey] = responseText ?? string.Empty,
        };

        if (string.IsNullOrWhiteSpace(responseText))
        {
            return output;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(responseText);
            if (parsed is not null)
            {
                foreach (var (key, value) in parsed)
                {
                    // Don't let a structured property clobber the reserved raw response key.
                    if (key == RawResponseKey)
                    {
                        continue;
                    }

                    output[key] = value;
                }
            }
        }
        catch (JsonException)
        {
            // Not valid JSON -- the raw text is already captured under RawResponseKey.
        }

        return output;
    }

    /// <summary>
    /// Resolves the configured media references into <see cref="DataContent"/> attachments.
    /// </summary>
    /// <remarks>
    /// Attachments are passed through as-is; the chat pipeline's file processing middleware
    /// converts supported documents and audio to text, and images reach the model directly.
    /// Any reference that can't be parsed, resolved, or that the service account can't access
    /// fails the step: running the agent without the attachment would leave it guessing at
    /// content it was meant to see.
    /// </remarks>
    private async Task<(IReadOnlyList<AIContent> Attachments, ActionResult? Failure)> ResolveAttachmentsAsync(
        string? attachmentsSetting,
        CancellationToken cancellationToken)
    {
        if (!MediaReferenceParser.TryParse(attachmentsSetting, out var mediaKeys, out var invalidReference))
        {
            return ([], ActionResult.Failed(
                new ArgumentException($"'{invalidReference}' is not a valid media reference. Use a media key, a media UDI, or a media picker value."),
                StepRunErrorCategory.Validation));
        }

        if (mediaKeys.Count > MaxAttachments)
        {
            return ([], ActionResult.Failed(
                new ArgumentException($"{mediaKeys.Count} attachments were supplied; the maximum is {MaxAttachments}."),
                StepRunErrorCategory.Validation));
        }

        var attachments = new List<AIContent>(mediaKeys.Count);
        long totalBytes = 0;

        foreach (var mediaKey in mediaKeys)
        {
            if (await _authorizer.AuthorizeMediaOrFailAsync(mediaKey, cancellationToken) is { } failure)
            {
                return ([], failure);
            }

            AIMediaContent? media = await _mediaResolver.ResolveAsync(mediaKey, cancellationToken: cancellationToken);
            if (media is null)
            {
                return ([], ActionResult.Failed(
                    new InvalidOperationException(
                        $"Could not resolve a file from media '{mediaKey}'. It may not exist, have no file, or be an unsupported file type."),
                    StepRunErrorCategory.Validation));
            }

            totalBytes += media.Data.Length;
            if (totalBytes > MaxTotalAttachmentBytes)
            {
                return ([], ActionResult.Failed(
                    new InvalidOperationException(
                        $"Attachments exceed the maximum combined size of {MaxTotalAttachmentBytes / (1024 * 1024)} MB."),
                    StepRunErrorCategory.Validation));
            }

            attachments.Add(new DataContent(media.Data, media.MediaType)
            {
                Name = _mediaService.GetById(mediaKey)?.Name,
            });
        }

        return (attachments, null);
    }

    private async Task<IUser?> ResolveServiceAccountAsync(ActionContext context)
    {
        var serviceAccountKey = context.ExecutionContext?.ServiceAccountKey;
        return serviceAccountKey is null || serviceAccountKey == Guid.Empty
            ? null
            : await _userService.GetAsync(serviceAccountKey.Value);
    }

    private static ActionResult RefusedFailure(string reason)
        => ActionResult.Failed(new UnauthorizedAccessException(reason), StepRunErrorCategory.Authentication);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ValidateSettingsForPublishAsync(
        object? settings,
        Automation automation,
        CancellationToken cancellationToken = default)
    {
        if (settings is not RunAgentSettings { AgentId: var agentId } || agentId == Guid.Empty)
        {
            return [];
        }

        var access = await _accessChecker.CheckForWorkspaceAsync(agentId, automation.WorkspaceId, cancellationToken);

        // The automation service prefixes the step name, so the message only adds the agent.
        return access.Status == AgentAccessStatus.Refused ? [access.RefusalReason] : [];
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

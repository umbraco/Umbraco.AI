using System.ComponentModel;
using Umbraco.AI.Automate.Actions;
using Umbraco.AI.Automate.Tools.Scopes;
using Umbraco.AI.Core.Tools;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Execution;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Automate.Tools;

/// <summary>
/// Arguments for the RunAutomation tool.
/// </summary>
/// <param name="AutomationId">The unique identifier of the automation to trigger.</param>
/// <param name="Message">Optional message or data to pass to the automation (used with AI Agent trigger).</param>
public record RunAutomationArgs(
    [property: Description("The unique identifier (GUID) of the automation to trigger. Use IDs from list_automations results.")]
    Guid AutomationId,

    [property: Description("Optional message or data to pass to the automation. Used when the automation has an AI Agent trigger.")]
    string? Message = null);

/// <summary>
/// Tool that triggers an automation and returns the run ID. The automation executes asynchronously
/// in the background — this tool returns immediately after the run is created.
/// </summary>
[AITool("run_automation", "Run Automation", ScopeId = AutomateExecuteScope.ScopeId, IsDestructive = true)]
public class RunAutomationTool : AIToolBase<RunAutomationArgs>
{
    private const string InitiatorType = "ai-agent";

    private readonly IAutomationService _automationService;
    private readonly IAutomationExecutor _automationExecutor;
    private readonly IWorkspaceService _workspaceService;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAutomationTool"/> class.
    /// </summary>
    public RunAutomationTool(
        IAutomationService automationService,
        IAutomationExecutor automationExecutor,
        IWorkspaceService workspaceService,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _automationService = automationService;
        _automationExecutor = automationExecutor;
        _workspaceService = workspaceService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public override string Description =>
        "Triggers an automation to run in the background and returns its run ID. " +
        "The automation executes asynchronously — use get_automation_run to check its progress if needed. " +
        "Only works with automations that have a Manual or AI Agent trigger.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(RunAutomationArgs args, CancellationToken cancellationToken = default)
    {
        if (args.AutomationId == Guid.Empty)
        {
            return new RunAutomationResult(false, null, null, "Automation ID cannot be empty.");
        }

        var user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            return new RunAutomationResult(false, null, null, "No authenticated user context available.");
        }

        Automation? automation = await _automationService.GetAutomationAsync(args.AutomationId, cancellationToken);
        if (automation is null)
        {
            return new RunAutomationResult(false, null, null, $"Automation '{args.AutomationId}' not found.");
        }

        // Verify the user has workspace access
        var userGroupKeys = user.Groups.Select(g => g.Key).ToList();
        var accessibleWorkspaceIds = await _workspaceService.GetAccessibleWorkspaceIdsAsync(userGroupKeys, cancellationToken);

        if (!accessibleWorkspaceIds.Contains(automation.WorkspaceId))
        {
            return new RunAutomationResult(false, null, automation.Name, "You do not have access to this automation's workspace.");
        }

        if (automation.Status != AutomationStatus.Published)
        {
            return new RunAutomationResult(false, null, automation.Name, "Automation is not published.");
        }

        if (!automation.IsEnabled)
        {
            return new RunAutomationResult(false, null, automation.Name, "Automation is not enabled.");
        }

        if (automation.Trigger is null || !UmbracoAIAutomateConstants.AgentInvokableTriggerAliases.Contains(
                automation.Trigger.TriggerAlias, StringComparer.OrdinalIgnoreCase))
        {
            return new RunAutomationResult(false, null, automation.Name,
                "Automation does not have a Manual or AI Agent trigger.");
        }

        // Build trigger output data.
        // Always include the nesting depth counter for the recursion guard in RunAgentAction.
        var triggerOutputData = new Dictionary<string, object?>
        {
            [RunAgentAction.AgentNestingDepthKey] = 1,
        };

        if (automation.Trigger.TriggerAlias.Equals(
                UmbracoAIAutomateConstants.TriggerTypes.AgentTrigger, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(args.Message))
        {
            triggerOutputData["message"] = args.Message;
        }

        var runId = await _automationExecutor.ExecuteAsync(
            automation,
            InitiatorType,
            user.Key.ToString(),
            triggerOutputData,
            cancellationToken);

        return new RunAutomationResult(true, runId, automation.Name, $"Automation '{automation.Name}' has been triggered.");
    }
}

/// <summary>
/// Result of the run automation tool.
/// </summary>
/// <param name="Success">Whether the automation was triggered successfully.</param>
/// <param name="RunId">The run ID for tracking progress, if triggered.</param>
/// <param name="AutomationName">The name of the automation.</param>
/// <param name="Message">Status message.</param>
public record RunAutomationResult(
    bool Success,
    Guid? RunId,
    string? AutomationName,
    string Message);

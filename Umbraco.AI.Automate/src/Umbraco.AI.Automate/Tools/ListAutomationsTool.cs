using System.ComponentModel;
using Umbraco.AI.Automate.Tools.Scopes;
using Umbraco.AI.Core.Tools;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Automate.Tools;

/// <summary>
/// Arguments for the ListAutomations tool.
/// </summary>
/// <param name="Filter">Optional name or alias filter.</param>
public record ListAutomationsArgs(
    [property: Description("Optional search filter to match automation names or aliases.")]
    string? Filter = null);

/// <summary>
/// Tool that lists automations the current user can trigger via AI agents.
/// Only returns published and enabled automations with Manual or AI Agent triggers
/// that the user has workspace access to.
/// </summary>
[AITool("list_automations", "List Automations", ScopeId = AutomateReadScope.ScopeId)]
public class ListAutomationsTool : AIToolBase<ListAutomationsArgs>
{
    private readonly IAutomationService _automationService;
    private readonly IWorkspaceService _workspaceService;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListAutomationsTool"/> class.
    /// </summary>
    public ListAutomationsTool(
        IAutomationService automationService,
        IWorkspaceService workspaceService,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _automationService = automationService;
        _workspaceService = workspaceService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public override string Description =>
        "Lists available automations that can be triggered. " +
        "Returns automations the current user has access to, filtered to those with Manual or AI Agent triggers. " +
        "Use this to discover which automations are available before triggering one with run_automation.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(ListAutomationsArgs args, CancellationToken cancellationToken = default)
    {
        var user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            return new ListAutomationsResult(false, [], "No authenticated user context available.");
        }

        var userGroupKeys = user.Groups.Select(g => g.Key).ToList();
        var accessibleWorkspaceIds = await _workspaceService.GetAccessibleWorkspaceIdsAsync(userGroupKeys, cancellationToken);

        if (accessibleWorkspaceIds.Count == 0)
        {
            return new ListAutomationsResult(true, [], null);
        }

        var (automations, _) = await _automationService.GetAutomationsPagedAsync(
            filter: args.Filter,
            workspaceIds: accessibleWorkspaceIds,
            skip: 0,
            take: 100,
            cancellationToken: cancellationToken);

        var items = automations
            .Where(a => a.Status == AutomationStatus.Published
                        && a.IsEnabled
                        && a.Trigger is not null
                        && UmbracoAIAutomateConstants.AgentInvokableTriggerAliases.Contains(
                            a.Trigger.TriggerAlias, StringComparer.OrdinalIgnoreCase))
            .Select(a => new AutomationItem(
                a.Id,
                a.Name,
                a.Alias,
                a.Description,
                a.Trigger!.TriggerAlias))
            .ToList();

        return new ListAutomationsResult(true, items, null);
    }
}

/// <summary>
/// Result of the list automations tool.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Automations">The list of available automations.</param>
/// <param name="Message">Optional message (typically for errors).</param>
public record ListAutomationsResult(
    bool Success,
    IReadOnlyList<AutomationItem> Automations,
    string? Message);

/// <summary>
/// An automation available for triggering.
/// </summary>
/// <param name="Id">The unique identifier of the automation.</param>
/// <param name="Name">The display name of the automation.</param>
/// <param name="Alias">The URL-safe alias of the automation.</param>
/// <param name="Description">A description of what the automation does.</param>
/// <param name="TriggerType">The trigger type alias (e.g. "umbracoAutomate.manual" or "umbracoAI.agentTrigger").</param>
public record AutomationItem(
    Guid Id,
    string Name,
    string Alias,
    string? Description,
    string TriggerType);

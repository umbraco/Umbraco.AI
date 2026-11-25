using Umbraco.AI.Agent.Core.Agents;
using Umbraco.Automate.Core.Security;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

namespace Umbraco.AI.Automate.Security;

/// <summary>
/// Resolves <c>onBehalfOf=automate-workspace:&lt;workspaceId&gt;</c> to the groups of the workspace's
/// service account, so the Run AI Agent step lists the agents that account can use when the automation runs.
/// </summary>
/// <remarks>
/// This is the authorization check for the value: the caller must be able to access the workspace
/// (an admin, or in one of its user groups, the same rule Automate applies to its own workspace endpoints)
/// before anything about its service account is revealed.
/// </remarks>
internal sealed class AutomateWorkspaceAgentUserGroupResolver : IAIAgentUserGroupResolver
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IWorkspaceServiceAccountResolver _serviceAccountResolver;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public AutomateWorkspaceAgentUserGroupResolver(
        IWorkspaceService workspaceService,
        IWorkspaceServiceAccountResolver serviceAccountResolver,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _workspaceService = workspaceService;
        _serviceAccountResolver = serviceAccountResolver;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    public string Kind => "automate-workspace";

    public async Task<Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>> ResolveUserGroupsAsync(
        string value,
        CancellationToken cancellationToken = default)
    {
        var caller = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;

        // A value that isn't a workspace id names no workspace the caller can access, so it is a refusal
        // rather than Malformed (which the dispatcher would turn into Forbidden anyway).
        if (caller is null || !Guid.TryParse(value, out var workspaceId) || workspaceId == Guid.Empty)
        {
            return Fail(AIAgentUserGroupResolutionStatus.Forbidden);
        }

        if (!await CanAccessWorkspaceAsync(caller, workspaceId, cancellationToken))
        {
            return Fail(AIAgentUserGroupResolutionStatus.Forbidden);
        }

        var serviceAccount = await _serviceAccountResolver.GetServiceAccountAsync(workspaceId, cancellationToken);
        if (serviceAccount is null)
        {
            return Fail(AIAgentUserGroupResolutionStatus.NoUser);
        }

        IReadOnlyList<Guid> groupKeys = serviceAccount.Groups.Select(g => g.Key).ToList();
        return Attempt.SucceedWithStatus(AIAgentUserGroupResolutionStatus.Success, groupKeys);
    }

    // Automate's own rule: admins see every workspace, everyone else needs a group the workspace lists.
    private async Task<bool> CanAccessWorkspaceAsync(
        IUser caller,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        if (caller.IsAdmin())
        {
            return true;
        }

        var accessible = await _workspaceService.GetAccessibleWorkspaceIdsAsync(
            caller.Groups.Select(g => g.Key).ToList(),
            cancellationToken);
        return accessible.Contains(workspaceId);
    }

    private static Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus> Fail(
        AIAgentUserGroupResolutionStatus status)
        => Attempt.FailWithStatus<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>(status, []);
}

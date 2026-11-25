using Umbraco.AI.Agent.Core.Agents;
using Umbraco.Automate.Core.Security;
using Umbraco.Cms.Core.Models.Membership;

namespace Umbraco.AI.Automate.Security;

/// <summary>
/// Decides whether a workspace's service account may use an agent, from the account's groups rather than the
/// ambient backoffice user (an automation run has none, or the wrong one).
/// </summary>
/// <remarks>
/// The limit itself is decided by <see cref="IAIAgentService"/>, so the rule lives in one place. The unrestricted
/// reader is only used to tell "no such agent" from "limited to other groups" and to name the agent in the reason.
/// </remarks>
internal sealed class AgentAccessChecker
{
    private readonly IAIAgentService _agentService;
    private readonly IAIAgentUnrestrictedReader _unrestrictedReader;
    private readonly IWorkspaceServiceAccountResolver _serviceAccountResolver;

    public AgentAccessChecker(
        IAIAgentService agentService,
        IAIAgentUnrestrictedReader unrestrictedReader,
        IWorkspaceServiceAccountResolver serviceAccountResolver)
    {
        _agentService = agentService;
        _unrestrictedReader = unrestrictedReader;
        _serviceAccountResolver = serviceAccountResolver;
    }

    /// <summary>
    /// Checks the agent against the service account of a workspace.
    /// </summary>
    public async Task<AgentAccess> CheckForWorkspaceAsync(
        Guid agentId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
        => await CheckAsync(
            agentId,
            await _serviceAccountResolver.GetServiceAccountAsync(workspaceId, cancellationToken),
            cancellationToken);

    /// <summary>
    /// Checks the agent against a service account. No service account means no groups, so only an
    /// agent without a limit passes.
    /// </summary>
    public async Task<AgentAccess> CheckAsync(
        Guid agentId,
        IUser? serviceAccount,
        CancellationToken cancellationToken = default)
    {
        var agent = await _unrestrictedReader.GetAgentUnrestrictedAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return new AgentAccess(AgentAccessStatus.NotFound, null, serviceAccount is null);
        }

        IReadOnlyCollection<Guid> groupKeys = serviceAccount?.Groups.Select(g => g.Key).ToList() ?? [];
        var usable = await _agentService.GetAgentAsync(agentId, groupKeys, cancellationToken);

        return new AgentAccess(
            usable is null ? AgentAccessStatus.Refused : AgentAccessStatus.Usable,
            agent.Alias,
            serviceAccount is null);
    }
}

/// <summary>
/// The outcome of an <see cref="AgentAccessChecker"/> check.
/// </summary>
internal enum AgentAccessStatus
{
    Usable,
    NotFound,
    Refused,
}

/// <summary>
/// What an agent check found, and the wording for a refusal.
/// </summary>
/// <param name="Status">Whether the agent exists and may be used.</param>
/// <param name="Alias">The agent's alias, when it exists.</param>
/// <param name="NoServiceAccount">Whether there was no service account to check against.</param>
internal sealed record AgentAccess(AgentAccessStatus Status, string? Alias, bool NoServiceAccount)
{
    /// <summary>
    /// The reason a refused agent can't be used.
    /// </summary>
    public string RefusalReason => NoServiceAccount
        ? $"The workspace has no service account, so it can't run agent '{Alias}', which is limited to user groups. Assign a service account, or pick another agent."
        : $"The workspace's service account can't use agent '{Alias}'. Add one of its user groups to the agent, or pick another agent.";
}

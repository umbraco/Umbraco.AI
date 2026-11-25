using Umbraco.AI.Agent.Copilot.Workspace.Core.Surfaces;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Surfaces;

namespace Umbraco.AI.Agent.Copilot.Workspace.Web.Agents;

/// <summary>
/// Resolves a conversation's stored agent choice and checks it against the Workspace surface. Shared by
/// the stream endpoint (which falls back to auto-selection for an unavailable agent) and the save-time
/// guard (which rejects one), so both read the stored value the same way.
/// </summary>
internal static class CopilotWorkspaceAgentAvailability
{
    /// <summary>The stored value that means "let the server pick an agent".</summary>
    public const string Auto = "auto";

    /// <summary>
    /// Whether a stored <c>AgentIdOrAlias</c> means auto-selection: null, empty/whitespace, or
    /// <c>"auto"</c> (any case).
    /// </summary>
    public static bool IsAuto(string? agentIdOrAlias)
        => string.IsNullOrWhiteSpace(agentIdOrAlias)
           || string.Equals(agentIdOrAlias, Auto, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Looks up a named agent by id (GUID) or alias. Returns null when it does not exist.
    /// </summary>
    public static async Task<AIAgent?> FindAgentAsync(
        IAIAgentService agentService,
        string agentIdOrAlias,
        CancellationToken cancellationToken)
        => Guid.TryParse(agentIdOrAlias, out var agentId)
            ? await agentService.GetAgentAsync(agentId, cancellationToken)
            : await agentService.GetAgentByAliasAsync(agentIdOrAlias, cancellationToken);

    /// <summary>
    /// Looks up a named agent by id (GUID) or alias so it can be run. Returns null when it does not exist.
    /// </summary>
    /// <exception cref="AIAgentNotAvailableException">The agent exists but is limited to user groups the current user is not in.</exception>
    public static async Task<AIAgent?> FindAgentForUseAsync(
        IAIAgentService agentService,
        string agentIdOrAlias,
        CancellationToken cancellationToken)
        => Guid.TryParse(agentIdOrAlias, out var agentId)
            ? await agentService.GetAgentForUseAsync(agentId, cancellationToken)
            : await agentService.GetAgentForUseByAliasAsync(agentIdOrAlias, cancellationToken);

    /// <summary>
    /// Whether the agent can run on the Copilot Workspace surface: active, opted in via
    /// <see cref="AIAgent.SurfaceIds"/>, and allowed by its scope rules. Delegates to the shared
    /// Agent.Core rule so the Workspace can't drift from the other surfaces.
    /// </summary>
    public static bool IsAvailable(
        AIAgent agent,
        AIAgentScopeValidator scopeValidator,
        AIAgentSurfaceCollection surfaces)
        => scopeValidator.IsAgentAvailableOnSurface(
            agent,
            CopilotWorkspaceAgentSurface.SurfaceId,
            new AgentAvailabilityContext { Surface = CopilotWorkspaceAgentSurface.SurfaceId },
            surfaces);
}

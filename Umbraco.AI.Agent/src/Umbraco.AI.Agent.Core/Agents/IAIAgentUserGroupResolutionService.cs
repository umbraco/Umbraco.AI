using Umbraco.Cms.Core;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Parses an <c>onBehalfOf</c> value and hands it to the resolver that claims its kind.
/// </summary>
internal interface IAIAgentUserGroupResolutionService
{
    /// <summary>
    /// Resolves an <c>onBehalfOf</c> value (<c>kind:value</c>) into user group keys.
    /// </summary>
    Task<Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>> ResolveUserGroupsAsync(
        string onBehalfOf,
        CancellationToken cancellationToken = default);
}

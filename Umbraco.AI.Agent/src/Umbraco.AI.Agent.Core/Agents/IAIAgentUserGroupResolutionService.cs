using Umbraco.Cms.Core;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Parses an <c>onBehalfOf</c> value and hands it to the resolver that claims its kind.
/// </summary>
/// <remarks>
/// Give it a <c>kind:value</c> string and it splits it at the first colon, finds the
/// <see cref="IAIAgentUserGroupResolver"/> that claims the kind, and returns the user group keys that resolver
/// produces. Failures come back as a failed result, not an exception: <c>Malformed</c> for a value that isn't
/// <c>kind:value</c>, <c>NotHandled</c> when no resolver claims the kind, <c>Forbidden</c> when the resolver
/// refuses because the caller may not act for the value, and <c>NoUser</c> when the value has no user to take
/// groups from. The resolver that claims the kind is the authorization check; this service adds none.
/// </remarks>
public interface IAIAgentUserGroupResolutionService
{
    /// <summary>
    /// Resolves an <c>onBehalfOf</c> value (<c>kind:value</c>) into user group keys.
    /// </summary>
    Task<Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>> ResolveUserGroupsAsync(
        string onBehalfOf,
        CancellationToken cancellationToken = default);
}

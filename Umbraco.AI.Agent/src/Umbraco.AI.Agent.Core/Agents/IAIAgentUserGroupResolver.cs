using Umbraco.Cms.Core;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Turns an <c>onBehalfOf</c> value (<c>kind:value</c>) into the user group keys to check agent access
/// against, instead of the caller's own groups.
/// </summary>
/// <remarks>
/// <para>
/// Register with <c>builder.AIAgentUserGroupResolvers().Append&lt;MyResolver&gt;()</c>. Each resolver claims one
/// <see cref="Kind"/>. Two resolvers claiming the same kind (case-insensitive) is a configuration error:
/// the first resolution throws <see cref="InvalidOperationException"/>. To override a kind, use
/// <c>Replace&lt;TOld, TNew&gt;()</c> or <c>Remove&lt;T&gt;()</c> on the collection builder.
/// </para>
/// <para>
/// <b>Security:</b> the resolver is the authorization check for <c>onBehalfOf</c>, and the caller does no
/// further check. A resolver MUST verify that the current backoffice user (from
/// <c>IBackOfficeSecurityAccessor</c>) may act for <c>value</c>, and return
/// <see cref="AIAgentUserGroupResolutionStatus.Forbidden"/> otherwise.
/// </para>
/// <para>
/// Resolvers are singletons (the collection is a singleton), so take only singleton dependencies or
/// create a scope yourself. Return only <see cref="AIAgentUserGroupResolutionStatus.Success"/>,
/// <see cref="AIAgentUserGroupResolutionStatus.Forbidden"/> or <see cref="AIAgentUserGroupResolutionStatus.NoUser"/>:
/// <see cref="AIAgentUserGroupResolutionStatus.Malformed"/> and <see cref="AIAgentUserGroupResolutionStatus.NotHandled"/>
/// belong to the dispatcher. Any other result, or a contradictory one, is treated as Forbidden.
/// </para>
/// </remarks>
public interface IAIAgentUserGroupResolver
{
    /// <summary>
    /// The kind this resolver claims: the text before the first <c>:</c> in <c>onBehalfOf</c>.
    /// Matched case-insensitively.
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Resolves the group keys for a value of this resolver's kind.
    /// </summary>
    /// <param name="value">The text after the first <c>:</c>. Never empty.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A successful attempt (<see cref="AIAgentUserGroupResolutionStatus.Success"/>) holding the group keys (an empty list is valid: a user in no groups), or a failed
    /// attempt with <see cref="AIAgentUserGroupResolutionStatus.Forbidden"/> when the current caller may not act
    /// for <paramref name="value"/>, or <see cref="AIAgentUserGroupResolutionStatus.NoUser"/> when it has no user.
    /// </returns>
    Task<Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>> ResolveUserGroupsAsync(
        string value,
        CancellationToken cancellationToken = default);
}

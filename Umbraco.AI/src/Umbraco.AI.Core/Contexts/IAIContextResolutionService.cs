using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Aggregates AI context from all registered resolvers and merges them into a single resolved context.
/// </summary>
/// <remarks>
/// <para>
/// Resolution uses all registered <see cref="IAIContextResolver"/> implementations in order.
/// Resources from later resolvers take precedence over earlier resolvers when duplicates exist.
/// </para>
/// <para>
/// Resolvers are registered via <see cref="AIContextResolverCollectionBuilder"/>:
/// </para>
/// <code>
/// builder.AIContextResolvers()
///     .Append&lt;ProfileContextResolver&gt;()
///     .InsertAfter&lt;ProfileContextResolver, AgentContextResolver&gt;();
/// </code>
/// </remarks>
public interface IAIContextResolutionService
{
    /// <summary>
    /// Resolves context from all registered resolvers.
    /// </summary>
    /// <remarks>
    /// This is the primary method used by middleware. Each registered resolver
    /// reads from <see cref="IAIRuntimeContextAccessor"/> to access request-scoped data.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The merged resolved context.</returns>
    Task<AIResolvedContext> ResolveContextAsync(CancellationToken cancellationToken = default);
}

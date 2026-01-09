using Umbraco.Ai.Core.Context.Resolvers;

namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Aggregates AI context from all registered resolvers and merges them into a single resolved context.
/// </summary>
/// <remarks>
/// <para>
/// Resolution uses all registered <see cref="IAiContextResolver"/> implementations in order.
/// Resources from later resolvers take precedence over earlier resolvers when duplicates exist.
/// </para>
/// <para>
/// Resolvers are registered via <see cref="AiContextResolverCollectionBuilder"/>:
/// </para>
/// <code>
/// builder.AiContextResolvers()
///     .Append&lt;ProfileContextResolver&gt;()
///     .InsertAfter&lt;ProfileContextResolver, AgentContextResolver&gt;();
/// </code>
/// </remarks>
public interface IAiContextResolutionService
{
    /// <summary>
    /// Resolves context from ChatOptions additional properties.
    /// </summary>
    /// <remarks>
    /// This is the primary method used by middleware. Each registered resolver
    /// reads its own keys from the properties dictionary.
    /// </remarks>
    /// <param name="properties">The additional properties from ChatOptions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The merged resolved context.</returns>
    Task<AiResolvedContext> ResolveAsync(
        IReadOnlyDictionary<string, object?>? properties,
        CancellationToken cancellationToken = default);
}

namespace Umbraco.AI.Core.Contexts.Resolvers;

/// <summary>
/// Result from a single context resolver.
/// </summary>
/// <remarks>
/// The aggregator combines results from all resolvers into a unified <see cref="AIResolvedContext"/>,
/// automatically setting <see cref="AIResolvedResource.Source"/> using the resolver's type name.
/// </remarks>
public sealed class AIContextResolverResult
{
    /// <summary>
    /// The resolved resources from this resolver.
    /// </summary>
    /// <remarks>
    /// These are converted to <see cref="AIResolvedResource"/> by the aggregator,
    /// which adds the <see cref="AIResolvedResource.Source"/> based on the resolver type name.
    /// </remarks>
    public IReadOnlyList<AIContextResolverResource> Resources { get; init; } = [];

    /// <summary>
    /// The context sources that were resolved (for tracking/debugging).
    /// </summary>
    /// <remarks>
    /// Each entry represents a context that was resolved, tracking the entity name
    /// (e.g., profile name) and context name for debugging purposes.
    /// </remarks>
    public IReadOnlyList<AIContextResolverSource> Sources { get; init; } = [];

    /// <summary>
    /// Returns an empty result (no resources resolved).
    /// </summary>
    public static AIContextResolverResult Empty => new();
}

namespace Umbraco.Ai.Core.Context.Resolvers;

/// <summary>
/// Result from a single context resolver.
/// </summary>
/// <remarks>
/// The aggregator combines results from all resolvers into a unified <see cref="AiResolvedContext"/>,
/// automatically setting <see cref="AiResolvedResource.Source"/> using the resolver's type name.
/// </remarks>
public sealed class AiContextResolverResult
{
    /// <summary>
    /// The resolved resources from this resolver.
    /// </summary>
    /// <remarks>
    /// These are converted to <see cref="AiResolvedResource"/> by the aggregator,
    /// which adds the <see cref="AiResolvedResource.Source"/> based on the resolver type name.
    /// </remarks>
    public IReadOnlyList<AiContextResolverResource> Resources { get; init; } = [];

    /// <summary>
    /// The context sources that were resolved (for tracking/debugging).
    /// </summary>
    /// <remarks>
    /// Each entry represents a context that was resolved, tracking the entity name
    /// (e.g., profile name) and context name for debugging purposes.
    /// </remarks>
    public IReadOnlyList<AiContextResolverSource> Sources { get; init; } = [];

    /// <summary>
    /// Returns an empty result (no resources resolved).
    /// </summary>
    public static AiContextResolverResult Empty => new();
}

namespace Umbraco.AI.Core.Contexts.Resolvers;

/// <summary>
/// Represents a resource returned by a context resolver before aggregation.
/// </summary>
/// <remarks>
/// This is similar to <see cref="AIResolvedResource"/> but without <c>Source</c>,
/// which is added automatically by the aggregator using the resolver's type name.
/// </remarks>
public sealed class AIContextResolverResource
{
    /// <summary>
    /// The unique identifier of the resource.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The immutable identifier of the resource type (e.g., "brand-voice", "text").
    /// </summary>
    public required string ResourceTypeId { get; init; }

    /// <summary>
    /// The display name of the resource.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of what this resource contains/provides.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Type-specific data object.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Determines how and when this resource is included in AI operations.
    /// </summary>
    public AIContextResourceInjectionMode InjectionMode { get; init; }

    /// <summary>
    /// The name of the context this resource came from.
    /// </summary>
    public required string ContextName { get; init; }
}

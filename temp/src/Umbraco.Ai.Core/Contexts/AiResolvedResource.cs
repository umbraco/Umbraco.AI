namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// Represents a resource after context resolution, including the source level it came from.
/// </summary>
public sealed class AiResolvedResource
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
    public AiContextResourceInjectionMode InjectionMode { get; init; }

    /// <summary>
    /// The source from which this resource was resolved.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The name of the context this resource came from.
    /// </summary>
    public required string ContextName { get; init; }
}

namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Represents a resource within an AI context, such as brand voice guidelines or reference text.
/// </summary>
public sealed class AiContextResource
{
    /// <summary>
    /// The unique identifier of the resource.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// The immutable identifier of the resource type (e.g., "brand-voice", "text").
    /// Links to <see cref="ResourceTypes.IAiContextResourceType.Id"/>.
    /// </summary>
    public required string ResourceTypeId { get; init; }

    /// <summary>
    /// The display name of the resource.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what this resource contains/provides.
    /// Used in UI and shown to LLM for OnDemand resources.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Controls injection order within the context.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// JSON blob containing the type-specific data.
    /// </summary>
    public required string Data { get; set; }

    /// <summary>
    /// Determines how and when this resource is included in AI operations.
    /// </summary>
    public AiContextResourceInjectionMode InjectionMode { get; set; } = AiContextResourceInjectionMode.Always;

    // V2: public float[]? Embedding { get; set; }  // For semantic injection mode
}

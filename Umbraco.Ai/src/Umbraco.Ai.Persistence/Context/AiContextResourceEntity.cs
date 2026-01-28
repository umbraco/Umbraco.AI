namespace Umbraco.Ai.Persistence.Context;

/// <summary>
/// EF Core entity representing a resource within an AI context.
/// </summary>
internal class AiContextResourceEntity
{
    /// <summary>
    /// Unique identifier for the resource.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the parent context.
    /// </summary>
    public Guid ContextId { get; set; }

    /// <summary>
    /// The resource type identifier (e.g., "brand-voice", "text").
    /// </summary>
    public string ResourceTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the resource.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the resource.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Sort order within the context.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// JSON-serialized resource data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Injection mode (0=Always, 1=OnDemand).
    /// </summary>
    public int InjectionMode { get; set; }

    /// <summary>
    /// Navigation property to the parent context.
    /// </summary>
    public AiContextEntity? Context { get; set; }
}

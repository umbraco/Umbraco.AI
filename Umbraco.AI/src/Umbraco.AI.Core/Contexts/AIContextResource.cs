namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Represents a resource within an AI context, such as brand voice guidelines or reference text.
/// </summary>
public sealed class AIContextResource
{
    private Guid _id;

    /// <summary>
    /// Creates a context resource with no ID. An ID is generated when it is first saved.
    /// </summary>
    public AIContextResource()
    {
    }

    /// <summary>
    /// Creates a context resource with a known ID.
    /// </summary>
    /// <param name="id">The ID the context resource is created with.</param>
    /// <remarks>
    /// Set the ID when creating a context resource that must keep the same ID across environments.
    /// Saving with an ID that already exists updates that context resource within the same context, instead of creating a new one.
    /// </remarks>
    public AIContextResource(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// The unique identifier of the resource.
    /// </summary>
    /// <remarks>
    /// Leave empty to have an ID generated when the context is saved. Set it when creating a
    /// resource that must keep an ID from elsewhere, such as one synced from another environment.
    /// </remarks>
    public Guid Id
    {
        get => _id;
        init => _id = value;
    }

    /// <summary>
    /// The immutable identifier of the resource type (e.g., "brand-voice", "text").
    /// Links to <see cref="ResourceTypes.IAIContextResourceType.Id"/>.
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
    /// Type-specific settings object configured by the user.
    /// </summary>
    public object? Settings { get; set; }

    /// <summary>
    /// Determines how and when this resource is included in AI operations.
    /// </summary>
    public AIContextResourceInjectionMode InjectionMode { get; set; } = AIContextResourceInjectionMode.Always;

    // V2: public float[]? Embedding { get; set; }  // For semantic injection mode

    /// <summary>
    /// Assigns the ID of a new resource. Used by the service when saving a resource with no ID.
    /// </summary>
    internal void SetId(Guid id) => _id = id;
}

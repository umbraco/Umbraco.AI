namespace Umbraco.Ai.Core.EntityAdapter;

/// <summary>
/// Represents a serialized entity for LLM context.
/// Contains the entity's identity and properties in a format suitable for AI processing.
/// </summary>
public sealed class AiSerializedEntity
{
    /// <summary>
    /// The entity type (e.g., "document", "media").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// The unique identifier of the entity.
    /// May be "new" for entities being created.
    /// </summary>
    public required string Unique { get; init; }

    /// <summary>
    /// The display name of the entity.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The content type alias (e.g., document type alias).
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// The unique identifier of the parent entity when creating a new entity.
    /// Null for existing entities or when parent is root.
    /// </summary>
    public string? ParentUnique { get; init; }

    /// <summary>
    /// The serialized properties of the entity.
    /// </summary>
    public IReadOnlyList<AiSerializedProperty> Properties { get; init; } = [];
}

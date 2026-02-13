using System.Text.Json;

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Represents a serialized entity for LLM context.
/// Contains the entity's identity and free-form data in a format suitable for AI processing.
/// </summary>
public sealed class AISerializedEntity
{
    /// <summary>
    /// The entity type (e.g., "document", "media", "commerce-product").
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
    /// The unique identifier of the parent entity when creating a new entity.
    /// Null for existing entities or when parent is root.
    /// </summary>
    public string? ParentUnique { get; init; }

    /// <summary>
    /// The entity data as a JSON object.
    /// Adapters decide the structure based on entity type.
    /// For CMS entities, this typically contains { contentType, properties }.
    /// For third-party entities, this can be any domain-appropriate JSON structure.
    /// </summary>
    public required JsonElement Data { get; init; }
}

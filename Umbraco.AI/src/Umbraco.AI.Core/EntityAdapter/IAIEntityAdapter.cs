namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Provides entity-type-specific formatting, metadata, and sub-type listing.
/// Implementations handle a single entity type (e.g., "document", "media").
/// Third parties register one adapter per custom entity type.
/// </summary>
public interface IAIEntityAdapter
{
    /// <summary>
    /// Gets the entity type this adapter handles (e.g., "document", "media").
    /// Returns null for the default/fallback adapter.
    /// </summary>
    string? EntityType { get; }

    /// <summary>
    /// Gets the display name for this entity type (e.g., "Document", "Media").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the icon for this entity type (e.g., "icon-document").
    /// </summary>
    string? Icon { get; }

    /// <summary>
    /// Gets whether this entity type has sub-types (e.g., content types for documents).
    /// When true, <see cref="GetEntitySubTypesAsync"/> returns available sub-types.
    /// The frontend uses this to conditionally show a sub-type picker.
    /// </summary>
    bool HasSubTypes { get; }

    /// <summary>
    /// Formats a serialized entity as a system message for LLM context.
    /// </summary>
    /// <param name="entity">The serialized entity to format.</param>
    /// <returns>Formatted markdown string suitable for LLM consumption.</returns>
    string FormatForLlm(AISerializedEntity entity);

    /// <summary>
    /// Lists sub-types for this entity type (e.g., content types for documents).
    /// Only called when <see cref="HasSubTypes"/> is true.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The available sub-types for this entity type.</returns>
    Task<IEnumerable<AIEntitySubType>> GetEntitySubTypesAsync(CancellationToken cancellationToken = default);
}

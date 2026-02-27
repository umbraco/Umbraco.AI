namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Formats serialized entities for LLM consumption.
/// Implementations can provide entity-type-specific formatting logic.
/// </summary>
public interface IAIEntityFormatter
{
    /// <summary>
    /// Gets the entity type this formatter handles.
    /// Returns null for the default/fallback formatter.
    /// </summary>
    string? EntityType { get; }

    /// <summary>
    /// Formats an entity for LLM context.
    /// </summary>
    /// <param name="entity">The serialized entity to format.</param>
    /// <returns>Formatted markdown string suitable for LLM consumption.</returns>
    string Format(AISerializedEntity entity);
}

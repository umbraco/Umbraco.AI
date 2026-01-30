namespace Umbraco.Ai.Core.EntityAdapter;

/// <summary>
/// Helper service for processing entity context data.
/// Provides utilities for building context dictionaries and formatting system messages.
/// </summary>
public interface IAiEntityContextHelper
{
    /// <summary>
    /// Builds a context dictionary from a serialized entity.
    /// The dictionary can be used for template variable replacement.
    /// </summary>
    /// <param name="entity">The serialized entity.</param>
    /// <returns>A dictionary with entity data suitable for template processing.</returns>
    Dictionary<string, object?> BuildContextDictionary(AiSerializedEntity entity);

    /// <summary>
    /// Formats a serialized entity as a system message for LLM context.
    /// </summary>
    /// <param name="entity">The serialized entity.</param>
    /// <returns>A formatted string describing the entity context.</returns>
    string FormatForLlm(AiSerializedEntity entity);
}

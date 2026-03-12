using System.Text.Json;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Configuration model for entity context in test features.
/// Stores the entity type, sub-type, and mock entity data.
/// </summary>
public sealed class EntityContextConfig
{
    /// <summary>
    /// The entity type (e.g., "document", "media", "member").
    /// </summary>
    public string EntityType { get; set; } = "document";

    /// <summary>
    /// The entity sub-type alias (e.g., content type alias "blogPost").
    /// Used by the frontend to restore UI state when reopening a test config.
    /// The backend does not use this at execution time.
    /// </summary>
    public string? EntitySubType { get; set; }

    /// <summary>
    /// The mock entity data as an AISerializedEntity JSON structure.
    /// Injected directly as an AIRequestContextItem at execution time.
    /// </summary>
    public JsonElement? MockEntity { get; set; }
}

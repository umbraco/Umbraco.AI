namespace Umbraco.Ai.Persistence.Connections;

/// <summary>
/// EF Core entity representing an AI provider connection.
/// </summary>
public class AiConnectionEntity
{
    /// <summary>
    /// Unique identifier for the connection.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique alias for the connection (used for programmatic lookup).
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the connection.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the provider this connection is for.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized provider-specific settings.
    /// </summary>
    public string? SettingsJson { get; set; }

    /// <summary>
    /// Whether this connection is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the connection was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// When the connection was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }
}

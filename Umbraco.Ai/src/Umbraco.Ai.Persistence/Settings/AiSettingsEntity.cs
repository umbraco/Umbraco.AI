namespace Umbraco.Ai.Persistence.Settings;

/// <summary>
/// EF Core entity for storing AI settings as key-value pairs.
/// </summary>
internal class AiSettingsEntity
{
    /// <summary>
    /// Unique identifier for the setting.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The setting key (e.g., "DefaultChatProfileId", "DefaultEmbeddingProfileId").
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The setting value (stored as string, typically a GUID).
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// When this setting was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// When this setting was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The key (GUID) of the user who created this setting.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this setting.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }
}

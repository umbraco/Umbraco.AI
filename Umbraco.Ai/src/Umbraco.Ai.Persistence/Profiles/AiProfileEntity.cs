namespace Umbraco.Ai.Persistence.Profiles;

/// <summary>
/// EF Core entity representing an AI profile configuration.
/// </summary>
internal class AiProfileEntity
{
    /// <summary>
    /// Unique identifier for the profile.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique alias for the profile (used for lookup).
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the profile.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The capability type (Chat=0, Embedding=1, Media=2, Moderation=3).
    /// </summary>
    public int Capability { get; set; }

    /// <summary>
    /// The ID of the AI provider.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the model within the provider.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the connection.
    /// </summary>
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// JSON-serialized capability-specific settings.
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Tags array serialized as a comma-separated string.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Current version of the profile.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// When the profile was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// When the profile was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The ID of the user who created this profile.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// The ID of the user who last modified this profile.
    /// </summary>
    public int? ModifiedByUserId { get; set; }
}

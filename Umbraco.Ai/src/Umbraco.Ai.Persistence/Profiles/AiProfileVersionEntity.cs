namespace Umbraco.Ai.Persistence.Profiles;

/// <summary>
/// EF Core entity representing a version snapshot of an AI profile.
/// </summary>
internal class AiProfileVersionEntity
{
    /// <summary>
    /// Unique identifier for the version record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the profile this version belongs to.
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// The version (1, 2, 3, etc.). Unique per profile.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// JSON serialization of the profile state at this version.
    /// </summary>
    public string Snapshot { get; set; } = string.Empty;

    /// <summary>
    /// When this version was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The user ID of the user who created this version.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// Optional description of what changed in this version.
    /// </summary>
    public string? ChangeDescription { get; set; }
}

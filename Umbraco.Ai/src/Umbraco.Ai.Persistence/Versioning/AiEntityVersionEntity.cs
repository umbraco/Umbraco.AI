namespace Umbraco.Ai.Persistence.Versioning;

/// <summary>
/// EF Core entity representing a version snapshot of any versionable AI entity.
/// </summary>
/// <remarks>
/// This unified entity replaces the separate version entities (AiConnectionVersionEntity,
/// AiProfileVersionEntity, AiContextVersionEntity) with a single table that uses
/// <see cref="EntityType"/> as a discriminator.
/// </remarks>
internal class AiEntityVersionEntity
{
    /// <summary>
    /// Unique identifier for the version record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the entity this version belongs to.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// The type of entity (e.g., "Connection", "Profile", "Context", "Prompt", "Agent").
    /// </summary>
    /// <remarks>
    /// This acts as a discriminator, allowing different entity types to share the same table.
    /// </remarks>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The version (1, 2, 3, etc.). Unique per entity.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// JSON serialization of the entity state at this version.
    /// </summary>
    /// <remarks>
    /// For entities with sensitive data (like Connection settings), this JSON contains
    /// encrypted values to maintain security in version history.
    /// </remarks>
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

namespace Umbraco.Ai.Core.Versioning;

/// <summary>
/// Represents a historical version of a versionable entity.
/// </summary>
/// <remarks>
/// Version snapshots are created automatically on each save operation.
/// The snapshot contains a JSON serialization of the entity state at that version.
/// </remarks>
public sealed class AiEntityVersion
{
    /// <summary>
    /// The unique identifier of this version record.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The ID of the entity this version belongs to.
    /// </summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// The type of entity this version belongs to (e.g., "Connection", "Profile", "Context").
    /// </summary>
    /// <remarks>
    /// This acts as a discriminator in the unified version table, allowing different entity types
    /// to share the same storage while maintaining distinct version histories.
    /// </remarks>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>
    /// The version (1, 2, 3, etc.). Unique per entity.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// JSON serialization of the entity state at this version.
    /// </summary>
    public string Snapshot { get; init; } = string.Empty;

    /// <summary>
    /// The date and time when this version was created.
    /// </summary>
    public DateTime DateCreated { get; init; }

    /// <summary>
    /// The key (GUID) of the user who created this version, if available.
    /// </summary>
    public Guid? CreatedByUserId { get; init; }

    /// <summary>
    /// Optional description of what changed in this version.
    /// </summary>
    public string? ChangeDescription { get; init; }
}

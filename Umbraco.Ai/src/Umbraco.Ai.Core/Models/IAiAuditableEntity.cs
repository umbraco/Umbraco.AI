namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Interface for entities that track audit information (creation and modification metadata).
/// </summary>
/// <remarks>
/// Implement this interface on entities that need to track who created/modified them and when.
/// For entities that also need version history with snapshots, implement <see cref="Versioning.IAiVersionableEntity"/> instead,
/// which extends this interface.
/// </remarks>
public interface IAiAuditableEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// When the entity was created.
    /// </summary>
    DateTime DateCreated { get; }

    /// <summary>
    /// When the entity was last modified.
    /// </summary>
    DateTime DateModified { get; }

    /// <summary>
    /// The key (GUID) of the user who created this entity.
    /// </summary>
    Guid? CreatedByUserId { get; }

    /// <summary>
    /// The key (GUID) of the user who last modified this entity.
    /// </summary>
    Guid? ModifiedByUserId { get; }
}

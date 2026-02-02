using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Versioning;

/// <summary>
/// Interface for entities that support versioning with snapshot history.
/// </summary>
/// <remarks>
/// <para>
/// Entities implementing this interface will have their changes tracked through version snapshots.
/// Each save operation creates a new version, allowing for audit trails and potential rollback.
/// </para>
/// <para>
/// This interface extends <see cref="IAIAuditableEntity"/> to ensure all versionable entities
/// also track audit information (creation/modification timestamps and user IDs).
/// </para>
/// </remarks>
public interface IAIVersionableEntity : IAIAuditableEntity
{
    /// <summary>
    /// The current version of the entity.
    /// Starts at 1 and increments with each save operation.
    /// </summary>
    int Version { get; }
}

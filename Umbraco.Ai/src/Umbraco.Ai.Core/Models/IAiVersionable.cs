namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Marker interface for entities that support versioning.
/// </summary>
/// <remarks>
/// Entities implementing this interface will have their changes tracked through version snapshots.
/// Each save operation creates a new version, allowing for audit trails and potential rollback.
/// </remarks>
public interface IAiVersionable
{
    /// <summary>
    /// The current version of the entity.
    /// Starts at 1 and increments with each save operation.
    /// </summary>
    int Version { get; }
}

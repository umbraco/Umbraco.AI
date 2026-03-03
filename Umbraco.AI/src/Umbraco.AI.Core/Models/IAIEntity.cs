namespace Umbraco.AI.Core.Models;

/// <summary>
/// Base interface for all Umbraco.AI entities that have a unique identifier.
/// </summary>
/// <remarks>
/// This is the root interface in the entity hierarchy. All entities that need persistent
/// identity should implement this interface, either directly or through derived interfaces
/// like <see cref="IAIAuditableEntity"/> or <see cref="Versioning.IAIVersionableEntity"/>.
/// </remarks>
public interface IAIEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    Guid Id { get; }
}

using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Versioning;

/// <summary>
/// Defines how a specific entity type participates in the unified versioning system.
/// </summary>
/// <remarks>
/// <para>
/// Each versionable entity type (Connection, Profile, Context, Prompt, Agent) implements this interface
/// to provide entity-specific snapshot creation, restoration, and comparison logic.
/// </para>
/// <para>
/// Implementations are registered via the <see cref="AIVersionableEntityAdapterCollectionBuilder"/> and
/// discovered at runtime by the <see cref="IAIEntityVersionService"/>.
/// </para>
/// </remarks>
public interface IAIVersionableEntityAdapter
{
    /// <summary>
    /// Gets the entity type name used as a discriminator in the unified version table.
    /// </summary>
    /// <remarks>
    /// This should be a short, stable identifier like "Connection", "Profile", "Context".
    /// It is stored in the database and used to route version operations to the correct handler.
    /// </remarks>
    string EntityTypeName { get; }

    /// <summary>
    /// Gets the CLR type of the entity this handler manages.
    /// </summary>
    Type EntityType { get; }

    /// <summary>
    /// Creates a JSON snapshot of the entity for version storage.
    /// </summary>
    /// <param name="entity">The entity to snapshot.</param>
    /// <returns>JSON string representing the entity state.</returns>
    /// <remarks>
    /// For entities with sensitive data (like Connection settings), the snapshot should
    /// include encrypted values to maintain security in version history.
    /// </remarks>
    string CreateSnapshot(object entity);

    /// <summary>
    /// Restores an entity from a JSON snapshot.
    /// </summary>
    /// <param name="json">The JSON snapshot.</param>
    /// <returns>The restored entity, or null if restoration fails.</returns>
    object? RestoreFromSnapshot(string json);

    /// <summary>
    /// Compares two entity versions and returns the list of value changes.
    /// </summary>
    /// <param name="from">The older entity version.</param>
    /// <param name="to">The newer entity version.</param>
    /// <returns>A list of value changes between the versions.</returns>
    IReadOnlyList<AIValueChange> CompareVersions(object from, object to);

    /// <summary>
    /// Rolls back an entity to a previous version.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="version">The version number to rollback to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the rollback is finished.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity or version is not found.
    /// </exception>
    /// <remarks>
    /// Rollback creates a new version with the state from the target version.
    /// The implementation should delegate to the entity's service for proper save logic.
    /// </remarks>
    Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of an entity from the main entity table.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    /// <remarks>
    /// This method retrieves the live entity state, which represents the current version
    /// that may not yet have a snapshot in the version history table.
    /// </remarks>
    Task<object?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken = default);
}

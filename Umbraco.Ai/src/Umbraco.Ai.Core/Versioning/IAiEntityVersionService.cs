using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Versioning;

/// <summary>
/// Service for managing entity versions in the unified versioning system.
/// </summary>
/// <remarks>
/// <para>
/// This service provides a single entry point for all version operations across all entity types.
/// It delegates entity-specific logic (snapshot creation, restoration, comparison) to the
/// appropriate <see cref="IAiVersionableEntityAdapter"/> handler.
/// </para>
/// <para>
/// Entity services (IAiConnectionService, IAiProfileService, etc.) can either:
/// </para>
/// <list type="bullet">
///   <item>Call this service directly for version operations</item>
///   <item>Provide convenience wrappers that delegate to this service</item>
/// </list>
/// </remarks>
public interface IAiEntityVersionService
{
    /// <summary>
    /// Gets the version history for an entity with pagination support.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type name (e.g., "Connection", "Profile").</param>
    /// <param name="skip">Number of versions to skip.</param>
    /// <param name="take">Maximum number of versions to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A tuple containing the paginated version history (ordered by version descending) and the total count.</returns>
    /// <exception cref="ArgumentException">Thrown if the entity type is not registered.</exception>
    Task<(IEnumerable<AiEntityVersion> Items, int Total)> GetVersionHistoryAsync(
        Guid entityId,
        string entityType,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version record for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type name (e.g., "Connection", "Profile").</param>
    /// <param name="version">The version number.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The version record, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown if the entity type is not registered.</exception>
    Task<AiEntityVersion?> GetVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version snapshot restored to its domain model.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="version">The version number.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The restored entity, or null if not found.</returns>
    Task<TEntity?> GetVersionSnapshotAsync<TEntity>(
        Guid entityId,
        int version,
        CancellationToken cancellationToken = default)
        where TEntity : class, IAiVersionableEntity;

    /// <summary>
    /// Saves a version snapshot for an entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to snapshot.</param>
    /// <param name="userId">The user key (GUID) of who created this version.</param>
    /// <param name="changeDescription">Optional description of what changed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <remarks>
    /// The snapshot is created using the registered <see cref="IAiVersionableEntityAdapter"/> handler.
    /// </remarks>
    Task SaveVersionAsync<TEntity>(
        TEntity entity,
        Guid? userId,
        string? changeDescription = null,
        CancellationToken cancellationToken = default)
        where TEntity : class, IAiVersionableEntity;

    /// <summary>
    /// Saves a version snapshot for an entity using raw snapshot data.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type name (e.g., "Connection", "Profile").</param>
    /// <param name="version">The version number.</param>
    /// <param name="snapshot">The JSON snapshot.</param>
    /// <param name="userId">The user key (GUID) of who created this version.</param>
    /// <param name="changeDescription">Optional description of what changed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        string snapshot,
        Guid? userId,
        string? changeDescription = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all versions for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type name (e.g., "Connection", "Profile").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <remarks>
    /// This should be called when an entity is deleted to clean up orphaned version records.
    /// </remarks>
    Task DeleteVersionsAsync(
        Guid entityId,
        string entityType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two versions of an entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type name (e.g., "Connection", "Profile").</param>
    /// <param name="fromVersion">The older version number.</param>
    /// <param name="toVersion">The newer version number.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The comparison result, or null if either version is not found.</returns>
    /// <exception cref="ArgumentException">Thrown if the entity type is not registered.</exception>
    Task<AiVersionComparison?> CompareVersionsAsync(
        Guid entityId,
        string entityType,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a snapshot of an entity without saving it.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to snapshot.</param>
    /// <returns>The JSON snapshot.</returns>
    /// <remarks>
    /// This is useful for repositories that need to create snapshots during save operations.
    /// </remarks>
    string CreateSnapshot<TEntity>(TEntity entity)
        where TEntity : class, IAiVersionableEntity;

    /// <summary>
    /// Restores an entity from a snapshot without querying the database.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="snapshot">The JSON snapshot.</param>
    /// <returns>The restored entity, or null if restoration fails.</returns>
    TEntity? RestoreFromSnapshot<TEntity>(string snapshot)
        where TEntity : class, IAiVersionableEntity;

    /// <summary>
    /// Cleans up old version records based on the configured policy.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the cleanup operation.</returns>
    /// <remarks>
    /// <para>
    /// The cleanup policy is configured via <see cref="AiVersionCleanupPolicy"/>.
    /// </para>
    /// <para>
    /// When both max versions and retention days are set, versions must satisfy
    /// BOTH conditions to be retained (AND logic). Age-based cleanup runs first,
    /// followed by count-based cleanup.
    /// </para>
    /// </remarks>
    Task<AiVersionCleanupResult> CleanupVersionsAsync(CancellationToken cancellationToken = default);
}

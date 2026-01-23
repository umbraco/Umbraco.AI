using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Versioning;

/// <summary>
/// Repository interface for unified entity version storage.
/// </summary>
/// <remarks>
/// This repository handles all version CRUD operations for the unified <c>umbracoAiEntityVersion</c> table.
/// It is an internal implementation detail of the <see cref="IAiEntityVersionService"/>.
/// </remarks>
internal interface IAiEntityVersionRepository
{
    /// <summary>
    /// Gets the version history for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type discriminator.</param>
    /// <param name="limit">Optional maximum number of versions to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The version history ordered by version descending (newest first).</returns>
    Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid entityId,
        string entityType,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version record for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type discriminator.</param>
    /// <param name="version">The version number.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The version record, or null if not found.</returns>
    Task<AiEntityVersion?> GetVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new version record.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type discriminator.</param>
    /// <param name="version">The version number.</param>
    /// <param name="snapshot">The JSON snapshot of the entity state.</param>
    /// <param name="userId">The user ID of who created this version.</param>
    /// <param name="changeDescription">Optional description of what changed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        string snapshot,
        int? userId,
        string? changeDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all versions for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type discriminator.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <remarks>
    /// This should be called when an entity is deleted to clean up orphaned version records.
    /// </remarks>
    Task DeleteVersionsAsync(
        Guid entityId,
        string entityType,
        CancellationToken cancellationToken = default);
}

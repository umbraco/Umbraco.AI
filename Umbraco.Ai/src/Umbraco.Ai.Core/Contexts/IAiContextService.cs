using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// Defines a contract for managing AI contexts.
/// </summary>
public interface IAiContextService
{
    /// <summary>
    /// Gets a specific context by its unique identifier.
    /// </summary>
    /// <param name="id">The context ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context if found, otherwise null.</returns>
    Task<AiContext?> GetContextAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific context by its alias.
    /// </summary>
    /// <param name="alias">The context alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context if found, otherwise null.</returns>
    Task<AiContext?> GetContextByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all contexts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All contexts.</returns>
    Task<IEnumerable<AiContext>> GetContextsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets contexts with pagination and optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter to search by name or alias (case-insensitive contains).</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated contexts and the total count.</returns>
    Task<(IEnumerable<AiContext> Items, int Total)> GetContextsPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (creates or updates) a context.
    /// </summary>
    /// <param name="context">The context to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved context.</returns>
    Task<AiContext> SaveContextAsync(AiContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a context by its unique identifier.
    /// </summary>
    /// <param name="id">The context ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteContextAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the version history for a context.
    /// </summary>
    /// <param name="contextId">The context ID.</param>
    /// <param name="limit">Optional limit on number of versions to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version history, ordered by version descending.</returns>
    Task<IEnumerable<AiEntityVersion>> GetContextVersionHistoryAsync(
        Guid contextId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version snapshot of a context.
    /// </summary>
    /// <param name="contextId">The context ID.</param>
    /// <param name="version">The version to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context at that version, or null if not found.</returns>
    Task<AiContext?> GetContextVersionSnapshotAsync(
        Guid contextId,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a context to a previous version.
    /// </summary>
    /// <param name="contextId">The context ID.</param>
    /// <param name="targetVersion">The version to rollback to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated context at the new version.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the context or target version is not found.</exception>
    Task<AiContext> RollbackContextAsync(
        Guid contextId,
        int targetVersion,
        CancellationToken cancellationToken = default);
}

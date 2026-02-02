using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Defines a repository for managing AI contexts.
/// Internal implementation detail - use <see cref="IAIContextService"/> for external access.
/// </summary>
internal interface IAIContextRepository
{
    /// <summary>
    /// Gets an AI context by its unique identifier.
    /// </summary>
    /// <param name="id">The context ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context if found, otherwise null.</returns>
    Task<AIContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an AI context by its alias.
    /// </summary>
    /// <param name="alias">The context alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context if found, otherwise null.</returns>
    Task<AIContext?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all AI contexts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All contexts.</returns>
    Task<IEnumerable<AIContext>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets AI contexts with pagination and optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter to search by name or alias (case-insensitive contains).</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated contexts and the total count.</returns>
    Task<(IEnumerable<AIContext> Items, int Total)> GetPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (creates or updates) an AI context.
    /// </summary>
    /// <param name="context">The context to save.</param>
    /// <param name="userId">Optional user key (GUID) for version tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved context.</returns>
    Task<AIContext> SaveAsync(AIContext context, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an AI context by its unique identifier.
    /// </summary>
    /// <param name="id">The context ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

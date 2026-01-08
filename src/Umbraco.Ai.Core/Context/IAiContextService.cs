namespace Umbraco.Ai.Core.Context;

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
}

using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Repository for managing AI provider connections.
/// Internal implementation detail - use <see cref="IAIConnectionService"/> for external access.
/// </summary>
internal interface IAIConnectionRepository
{
    /// <summary>
    /// Get a connection by its ID.
    /// </summary>
    Task<AIConnection?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a connection by its alias (case-insensitive).
    /// </summary>
    Task<AIConnection?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections.
    /// </summary>
    Task<IEnumerable<AIConnection>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connections with pagination and optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter to search by name (case-insensitive contains).</param>
    /// <param name="providerId">Optional provider ID to filter by.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated connections and the total count.</returns>
    Task<(IEnumerable<AIConnection> Items, int Total)> GetPagedAsync(
        string? filter = null,
        string? providerId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections for a specific provider.
    /// </summary>
    Task<IEnumerable<AIConnection>> GetByProviderAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a connection (insert if new, update if exists).
    /// </summary>
    /// <param name="connection">The connection to save.</param>
    /// <param name="userId">The key (GUID) of the user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AIConnection> SaveAsync(AIConnection connection, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a connection by ID.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a connection exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

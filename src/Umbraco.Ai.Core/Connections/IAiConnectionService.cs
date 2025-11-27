using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Connections;

/// <summary>
/// Service for managing AI provider connections with validation and business logic.
/// </summary>
public interface IAiConnectionService
{
    /// <summary>
    /// Get a connection by ID.
    /// </summary>
    Task<AiConnection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a connection by alias (case-insensitive).
    /// </summary>
    Task<AiConnection?> GetConnectionByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections, optionally filtered by provider.
    /// </summary>
    Task<IEnumerable<AiConnection>> GetConnectionsAsync(string? providerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connections with pagination and optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter to search by name (case-insensitive contains).</param>
    /// <param name="providerId">Optional provider ID to filter by.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated connections and the total count.</returns>
    Task<(IEnumerable<AiConnection> Items, int Total)> GetConnectionsPagedAsync(
        string? filter = null,
        string? providerId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connection references for a provider (lightweight list for UI).
    /// </summary>
    Task<IEnumerable<AiConnectionRef>> GetConnectionReferencesAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a connection (insert if new, update if exists) with validation.
    /// If connection.Id is Guid.Empty, a new Guid will be generated.
    /// </summary>
    Task<AiConnection> SaveConnectionAsync(AiConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a connection (with usage checks).
    /// </summary>
    Task DeleteConnectionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate connection settings against provider schema.
    /// </summary>
    Task<bool> ValidateConnectionAsync(string providerId, object? settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test a connection by attempting to fetch models (if supported).
    /// </summary>
    Task<bool> TestConnectionAsync(Guid id, CancellationToken cancellationToken = default);
}

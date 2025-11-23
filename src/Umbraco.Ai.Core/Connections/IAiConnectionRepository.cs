using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Connections;

/// <summary>
/// Repository for managing AI provider connections.
/// </summary>
public interface IAiConnectionRepository
{
    /// <summary>
    /// Get a connection by its ID.
    /// </summary>
    Task<AiConnection?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections.
    /// </summary>
    Task<IEnumerable<AiConnection>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections for a specific provider.
    /// </summary>
    Task<IEnumerable<AiConnection>> GetByProviderAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a connection (insert if new, update if exists).
    /// </summary>
    Task<AiConnection> SaveAsync(AiConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a connection by ID.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a connection exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

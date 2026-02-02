using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Service for managing AI provider connections with validation and business logic.
/// </summary>
public interface IAIConnectionService
{
    /// <summary>
    /// Get a connection by ID.
    /// </summary>
    Task<AIConnection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a connection by alias (case-insensitive).
    /// </summary>
    Task<AIConnection?> GetConnectionByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections, optionally filtered by provider.
    /// </summary>
    Task<IEnumerable<AIConnection>> GetConnectionsAsync(string? providerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connections with pagination and optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter to search by name (case-insensitive contains).</param>
    /// <param name="providerId">Optional provider ID to filter by.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated connections and the total count.</returns>
    Task<(IEnumerable<AIConnection> Items, int Total)> GetConnectionsPagedAsync(
        string? filter = null,
        string? providerId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connection references for a provider (lightweight list for UI).
    /// </summary>
    Task<IEnumerable<AIConnectionRef>> GetConnectionReferencesAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a connection (insert if new, update if exists) with validation.
    /// If connection.Id is Guid.Empty, a new Guid will be generated.
    /// </summary>
    Task<AIConnection> SaveConnectionAsync(AIConnection connection, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Gets the unique capabilities available across all configured connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of capabilities that are available from at least one connection.</returns>
    Task<IEnumerable<Models.AICapability>> GetAvailableCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets connections that support a specific capability.
    /// </summary>
    /// <param name="capability">The capability to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connections whose providers support the specified capability.</returns>
    Task<IEnumerable<AIConnection>> GetConnectionsByCapabilityAsync(Models.AICapability capability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configured provider for a connection with resolved settings.
    /// This is the primary way to interact with provider capabilities.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured provider with resolved settings, or null if connection/provider not found.</returns>
    Task<IAIConfiguredProvider?> GetConfiguredProviderAsync(Guid connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the version history for a connection.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <param name="skip">Number of versions to skip.</param>
    /// <param name="take">Maximum number of versions to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the paginated version history (ordered by version descending) and the total count.</returns>
    Task<(IEnumerable<AIEntityVersion> Items, int Total)> GetConnectionVersionHistoryAsync(
        Guid connectionId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version snapshot of a connection.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <param name="version">The version to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection at that version, or null if not found.</returns>
    Task<AIConnection?> GetConnectionVersionSnapshotAsync(
        Guid connectionId,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a connection to a previous version.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <param name="targetVersion">The version to rollback to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated connection at the new version.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection or target version is not found.</exception>
    Task<AIConnection> RollbackConnectionAsync(
        Guid connectionId,
        int targetVersion,
        CancellationToken cancellationToken = default);
}

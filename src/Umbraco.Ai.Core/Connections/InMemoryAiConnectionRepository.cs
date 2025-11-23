using System.Collections.Concurrent;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Connections;

/// <summary>
/// In-memory implementation of connection repository (for prototyping).
/// TODO: Replace with database-backed implementation.
/// </summary>
internal sealed class InMemoryAiConnectionRepository : IAiConnectionRepository
{
    private readonly ConcurrentDictionary<Guid, AiConnection> _connections = new();

    /// <inheritdoc />
    public Task<AiConnection?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _connections.TryGetValue(id, out var connection);
        return Task.FromResult(connection);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiConnection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AiConnection>>(_connections.Values.ToList());
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiConnection>> GetByProviderAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var connections = _connections.Values
            .Where(c => string.Equals(c.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<AiConnection>>(connections);
    }

    /// <inheritdoc />
    public Task<AiConnection> SaveAsync(AiConnection connection, CancellationToken cancellationToken = default)
    {
        var isUpdate = _connections.ContainsKey(connection.Id);

        if (isUpdate)
        {
            // Update existing connection
            connection.DateModified = DateTime.UtcNow;
        }

        _connections[connection.Id] = connection;
        return Task.FromResult(connection);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _connections.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = _connections.ContainsKey(id);
        return Task.FromResult(exists);
    }
}

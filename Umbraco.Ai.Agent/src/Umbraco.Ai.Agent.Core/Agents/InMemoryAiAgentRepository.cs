using System.Collections.Concurrent;
using Umbraco.Ai.Core.Models;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// In-memory implementation of <see cref="IAiAgentRepository"/> for testing and fallback scenarios.
/// </summary>
internal sealed class InMemoryAiAgentRepository : IAiAgentRepository
{
    private readonly ConcurrentDictionary<Guid, AiAgent> _agents = new();

    /// <inheritdoc />
    public Task<AiAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _agents.TryGetValue(id, out var agent);
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public Task<AiAgent?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var agent = _agents.Values.FirstOrDefault(p =>
            p.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiAgent>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<AiAgent>>(_agents.Values.ToList());

    /// <inheritdoc />
    public Task<IEnumerable<AiAgent>> GetByProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var agents = _agents.Values
            .Where(p => p.ProfileId == profileId)
            .ToList();
        return Task.FromResult<IEnumerable<AiAgent>>(agents);
    }

    /// <inheritdoc />
    public Task<PagedModel<AiAgent>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _agents.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(p =>
                p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                p.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        if (profileId.HasValue)
        {
            query = query.Where(p => p.ProfileId == profileId.Value);
        }

        var total = query.Count();
        var items = query
            .OrderBy(p => p.Name)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult(new PagedModel<AiAgent>(total, items));
    }

    /// <inheritdoc />
    public Task<AiAgent> SaveAsync(AiAgent agent, int? userId = null, CancellationToken cancellationToken = default)
    {
        _agents[agent.Id] = agent;
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_agents.TryRemove(id, out _));

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_agents.ContainsKey(id));

    /// <inheritdoc />
    public Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _agents.Values.Any(p =>
            p.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || p.Id != excludeId.Value));
        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid agentId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        // In-memory repository doesn't track version history
        return Task.FromResult<IEnumerable<AiEntityVersion>>([]);
    }

    /// <inheritdoc />
    public Task<AiAgent?> GetVersionSnapshotAsync(
        Guid agentId,
        int version,
        CancellationToken cancellationToken = default)
    {
        // In-memory repository doesn't track version history
        return Task.FromResult<AiAgent?>(null);
    }
}

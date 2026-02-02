using System.Collections.Concurrent;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// In-memory implementation of <see cref="IAIAgentRepository"/> for testing and fallback scenarios.
/// </summary>
internal sealed class InMemoryAIAgentRepository : IAIAgentRepository
{
    private readonly ConcurrentDictionary<Guid, AIAgent> _agents = new();

    /// <inheritdoc />
    public Task<AIAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _agents.TryGetValue(id, out var agent);
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public Task<AIAgent?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var agent = _agents.Values.FirstOrDefault(p =>
            p.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AIAgent>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<AIAgent>>(_agents.Values.ToList());

    /// <inheritdoc />
    public Task<IEnumerable<AIAgent>> GetByProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var agents = _agents.Values
            .Where(p => p.ProfileId == profileId)
            .ToList();
        return Task.FromResult<IEnumerable<AIAgent>>(agents);
    }

    /// <inheritdoc />
    public Task<PagedModel<AIAgent>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        string? scopeId = null,
        bool? isActive = null,
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

        if (!string.IsNullOrWhiteSpace(scopeId))
        {
            query = query.Where(p => p.ScopeIds.Contains(scopeId, StringComparer.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var total = query.Count();
        var items = query
            .OrderBy(p => p.Name)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult(new PagedModel<AIAgent>(total, items));
    }

    /// <inheritdoc />
    public Task<IEnumerable<AIAgent>> GetByScopeAsync(string scopeId, CancellationToken cancellationToken = default)
    {
        var agents = _agents.Values
            .Where(p => p.ScopeIds.Contains(scopeId, StringComparer.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<AIAgent>>(agents);
    }

    /// <inheritdoc />
    public Task<AIAgent> SaveAsync(AIAgent agent, Guid? userId = null, CancellationToken cancellationToken = default)
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
    public Task<IEnumerable<AIEntityVersion>> GetVersionHistoryAsync(
        Guid agentId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        // In-memory repository doesn't track version history
        return Task.FromResult<IEnumerable<AIEntityVersion>>([]);
    }

    /// <inheritdoc />
    public Task<AIAgent?> GetVersionSnapshotAsync(
        Guid agentId,
        int version,
        CancellationToken cancellationToken = default)
    {
        // In-memory repository doesn't track version history
        return Task.FromResult<AIAgent?>(null);
    }
}

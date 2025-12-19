using System.Collections.Concurrent;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// In-memory implementation of <see cref="IAiAgentRepository"/> for testing and fallback scenarios.
/// </summary>
internal sealed class InMemoryAiAgentRepository : IAiAgentRepository
{
    private readonly ConcurrentDictionary<Guid, AiAgent> _Agents = new();

    /// <inheritdoc />
    public Task<AiAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _Agents.TryGetValue(id, out var prompt);
        return Task.FromResult(prompt);
    }

    /// <inheritdoc />
    public Task<AiAgent?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var prompt = _Agents.Values.FirstOrDefault(p =>
            p.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(prompt);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiAgent>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<AiAgent>>(_Agents.Values.ToList());

    /// <inheritdoc />
    public Task<IEnumerable<AiAgent>> GetByProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var Agents = _Agents.Values
            .Where(p => p.ProfileId == profileId)
            .ToList();
        return Task.FromResult<IEnumerable<AiAgent>>(Agents);
    }

    /// <inheritdoc />
    public Task<PagedModel<AiAgent>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _Agents.Values.AsEnumerable();

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
    public Task<AiAgent> SaveAsync(AiAgent AiAgent, CancellationToken cancellationToken = default)
    {
        _Agents[AiAgent.Id] = AiAgent;
        return Task.FromResult(AiAgent);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_Agents.TryRemove(id, out _));

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_Agents.ContainsKey(id));

    /// <inheritdoc />
    public Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _Agents.Values.Any(p =>
            p.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || p.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}

using System.Collections.Concurrent;
using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// In-memory implementation of <see cref="IAIOrchestrationRepository"/> for testing and fallback scenarios.
/// </summary>
internal sealed class InMemoryAIOrchestrationRepository : IAIOrchestrationRepository
{
    private readonly ConcurrentDictionary<Guid, AIOrchestration> _orchestrations = new();

    /// <inheritdoc />
    public Task<AIOrchestration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _orchestrations.TryGetValue(id, out var orchestration);
        return Task.FromResult(orchestration);
    }

    /// <inheritdoc />
    public Task<AIOrchestration?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var orchestration = _orchestrations.Values.FirstOrDefault(o =>
            o.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(orchestration);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AIOrchestration>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<AIOrchestration>>(_orchestrations.Values.ToList());

    /// <inheritdoc />
    public Task<PagedModel<AIOrchestration>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        string? surfaceId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _orchestrations.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(o =>
                o.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                o.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(surfaceId))
        {
            query = query.Where(o => o.SurfaceIds.Contains(surfaceId, StringComparer.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(o => o.IsActive == isActive.Value);
        }

        var total = query.Count();
        var items = query
            .OrderBy(o => o.Name)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult(new PagedModel<AIOrchestration>(total, items));
    }

    /// <inheritdoc />
    public Task<IEnumerable<AIOrchestration>> GetBySurfaceAsync(string surfaceId, CancellationToken cancellationToken = default)
    {
        var orchestrations = _orchestrations.Values
            .Where(o => o.SurfaceIds.Contains(surfaceId, StringComparer.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<AIOrchestration>>(orchestrations);
    }

    /// <inheritdoc />
    public Task<AIOrchestration> SaveAsync(AIOrchestration orchestration, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _orchestrations[orchestration.Id] = orchestration;
        return Task.FromResult(orchestration);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_orchestrations.TryRemove(id, out _));

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_orchestrations.ContainsKey(id));

    /// <inheritdoc />
    public Task<bool> ExistsWithProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
        => Task.FromResult(_orchestrations.Values.Any(o => o.ProfileId == profileId));

    /// <inheritdoc />
    public Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _orchestrations.Values.Any(o =>
            o.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || o.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}

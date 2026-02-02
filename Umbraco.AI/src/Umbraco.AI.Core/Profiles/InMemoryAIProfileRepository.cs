using System.Collections.Concurrent;
using Umbraco.AI.Core.Models;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Profiles;

internal sealed class InMemoryAiProfileRepository : IAIProfileRepository
{
    private readonly ConcurrentDictionary<Guid, AIProfile> _profiles = new();

    public Task<AIProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(id, out var profile);
        return Task.FromResult(profile);
    }
    
    public Task<AIProfile?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var profile = _profiles.Values.FirstOrDefault(x => x.Alias.InvariantEquals(alias));
        return Task.FromResult(profile);
    }

    public Task<IEnumerable<AIProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AIProfile>>(_profiles.Values.ToList());
    }

    public Task<IEnumerable<AIProfile>> GetByCapability(AICapability capability, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AIProfile>>(_profiles.Values.Where(x => x.Capability == capability).ToList());
    }

    public Task<(IEnumerable<AIProfile> Items, int Total)> GetPagedAsync(
        string? filter = null,
        AICapability? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AIProfile> query = _profiles.Values;

        if (capability.HasValue)
        {
            query = query.Where(p => p.Capability == capability.Value);
        }

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(p => p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var items = query.OrderBy(p => p.Name).ToList();
        var total = items.Count;
        var pagedItems = items.Skip(skip).Take(take);

        return Task.FromResult<(IEnumerable<AIProfile> Items, int Total)>((pagedItems, total));
    }

    public Task<AIProfile> SaveAsync(AIProfile profile, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _profiles[profile.Id] = profile;
        return Task.FromResult(profile);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_profiles.TryRemove(id, out _));
    }
}
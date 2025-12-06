using System.Collections.Concurrent;
using Umbraco.Ai.Core.Models;
using Umbraco.Extensions;

namespace Umbraco.Ai.Core.Profiles;

internal sealed class InMemoryAiProfileRepository : IAiProfileRepository
{
    private readonly ConcurrentDictionary<Guid, AiProfile> _profiles = new();

    public Task<AiProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(id, out var profile);
        return Task.FromResult(profile);
    }
    
    public Task<AiProfile?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var profile = _profiles.Values.FirstOrDefault(x => x.Alias.InvariantEquals(alias));
        return Task.FromResult(profile);
    }

    public Task<IEnumerable<AiProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AiProfile>>(_profiles.Values.ToList());
    }

    public Task<IEnumerable<AiProfile>> GetByCapability(AiCapability capability, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AiProfile>>(_profiles.Values.Where(x => x.Capability == capability).ToList());
    }

    public Task<(IEnumerable<AiProfile> Items, int Total)> GetPagedAsync(
        string? filter = null,
        AiCapability? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AiProfile> query = _profiles.Values;

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

        return Task.FromResult<(IEnumerable<AiProfile> Items, int Total)>((pagedItems, total));
    }

    public Task<AiProfile> SaveAsync(AiProfile profile, CancellationToken cancellationToken = default)
    {
        _profiles[profile.Id] = profile;
        return Task.FromResult(profile);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_profiles.TryRemove(id, out _));
    }
}
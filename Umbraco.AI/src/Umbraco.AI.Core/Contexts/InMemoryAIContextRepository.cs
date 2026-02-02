using System.Collections.Concurrent;
using Umbraco.AI.Core.Models;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// In-memory implementation of <see cref="IAiContextRepository"/> for development/testing.
/// </summary>
internal sealed class InMemoryAiContextRepository : IAiContextRepository
{
    private readonly ConcurrentDictionary<Guid, AIContext> _contexts = new();

    /// <inheritdoc />
    public Task<AIContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _contexts.TryGetValue(id, out var context);
        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public Task<AIContext?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var context = _contexts.Values.FirstOrDefault(x => x.Alias.InvariantEquals(alias));
        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AIContext>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AIContext>>(_contexts.Values.ToList());
    }

    /// <inheritdoc />
    public Task<(IEnumerable<AIContext> Items, int Total)> GetPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AIContext> query = _contexts.Values;

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(c =>
                c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var items = query.OrderBy(c => c.Name).ToList();
        var total = items.Count;
        var pagedItems = items.Skip(skip).Take(take);

        return Task.FromResult<(IEnumerable<AIContext> Items, int Total)>((pagedItems, total));
    }

    /// <inheritdoc />
    public Task<AIContext> SaveAsync(AIContext context, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _contexts[context.Id] = context;
        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_contexts.TryRemove(id, out _));
    }
}

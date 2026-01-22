using System.Collections.Concurrent;
using Umbraco.Ai.Core.Models;
using Umbraco.Extensions;

namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// In-memory implementation of <see cref="IAiContextRepository"/> for development/testing.
/// </summary>
internal sealed class InMemoryAiContextRepository : IAiContextRepository
{
    private readonly ConcurrentDictionary<Guid, AiContext> _contexts = new();

    /// <inheritdoc />
    public Task<AiContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _contexts.TryGetValue(id, out var context);
        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public Task<AiContext?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var context = _contexts.Values.FirstOrDefault(x => x.Alias.InvariantEquals(alias));
        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiContext>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AiContext>>(_contexts.Values.ToList());
    }

    /// <inheritdoc />
    public Task<(IEnumerable<AiContext> Items, int Total)> GetPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AiContext> query = _contexts.Values;

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(c =>
                c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var items = query.OrderBy(c => c.Name).ToList();
        var total = items.Count;
        var pagedItems = items.Skip(skip).Take(take);

        return Task.FromResult<(IEnumerable<AiContext> Items, int Total)>((pagedItems, total));
    }

    /// <inheritdoc />
    public Task<AiContext> SaveAsync(AiContext context, int? userId = null, CancellationToken cancellationToken = default)
    {
        _contexts[context.Id] = context;
        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_contexts.TryRemove(id, out _));
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid contextId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        // In-memory repository doesn't track version history
        return Task.FromResult<IEnumerable<AiEntityVersion>>([]);
    }

    /// <inheritdoc />
    public Task<AiContext?> GetVersionSnapshotAsync(
        Guid contextId,
        int version,
        CancellationToken cancellationToken = default)
    {
        // In-memory repository doesn't track version history
        return Task.FromResult<AiContext?>(null);
    }
}

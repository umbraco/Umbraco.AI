using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Versioning;

namespace Umbraco.Ai.Tests.Common.Fakes;

/// <summary>
/// Fake in-memory implementation of <see cref="IAiContextRepository"/> for use in tests.
/// </summary>
public class FakeAiContextRepository : IAiContextRepository
{
    private readonly Dictionary<Guid, AiContext> _contexts = new();

    /// <summary>
    /// Gets all contexts stored in this fake repository.
    /// </summary>
    public IReadOnlyDictionary<Guid, AiContext> Contexts => _contexts;

    /// <summary>
    /// Seeds the repository with initial contexts.
    /// </summary>
    public FakeAiContextRepository WithContexts(params AiContext[] contexts)
    {
        foreach (var context in contexts)
        {
            _contexts[context.Id] = context;
        }
        return this;
    }

    /// <inheritdoc />
    public Task<AiContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _contexts.TryGetValue(id, out var context);
        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public Task<AiContext?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var context = _contexts.Values.FirstOrDefault(c =>
            c.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
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
        var query = _contexts.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(c =>
                c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var total = query.Count();
        var items = query.Skip(skip).Take(take).ToList();

        return Task.FromResult<(IEnumerable<AiContext> Items, int Total)>((items, total));
    }

    /// <inheritdoc />
    public Task<AiContext> SaveAsync(AiContext context, int? userId = null, CancellationToken cancellationToken = default)
    {
        // For fakes, we expect the context to already have an ID set by the builder
        // DateCreated is init-only and set at construction time
        context.DateModified = DateTime.UtcNow;
        _contexts[context.Id] = context;

        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_contexts.Remove(id));
    }

    /// <summary>
    /// Clears all contexts from the repository.
    /// </summary>
    public void Clear()
    {
        _contexts.Clear();
    }

    /// <inheritdoc />
    public Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid contextId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        // Fake repository doesn't track version history
        return Task.FromResult<IEnumerable<AiEntityVersion>>([]);
    }

    /// <inheritdoc />
    public Task<AiContext?> GetVersionSnapshotAsync(
        Guid contextId,
        int version,
        CancellationToken cancellationToken = default)
    {
        // Fake repository doesn't track version history
        return Task.FromResult<AiContext?>(null);
    }
}

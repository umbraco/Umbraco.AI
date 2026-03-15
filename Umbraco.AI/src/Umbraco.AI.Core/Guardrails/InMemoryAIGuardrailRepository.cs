using System.Collections.Concurrent;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// In-memory implementation of <see cref="IAIGuardrailRepository"/> for development/testing.
/// </summary>
internal sealed class InMemoryAIGuardrailRepository : IAIGuardrailRepository
{
    private readonly ConcurrentDictionary<Guid, AIGuardrail> _guardrails = new();

    /// <inheritdoc />
    public Task<AIGuardrail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _guardrails.TryGetValue(id, out var guardrail);
        return Task.FromResult(guardrail);
    }

    /// <inheritdoc />
    public Task<AIGuardrail?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var guardrail = _guardrails.Values.FirstOrDefault(x => x.Alias.InvariantEquals(alias));
        return Task.FromResult(guardrail);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AIGuardrail>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AIGuardrail>>(_guardrails.Values.ToList());
    }

    /// <inheritdoc />
    public Task<(IEnumerable<AIGuardrail> Items, int Total)> GetPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AIGuardrail> query = _guardrails.Values;

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(g =>
                g.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                g.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var all = query.OrderBy(g => g.Name).ToList();
        var items = all.Skip(skip).Take(take).ToList();
        return Task.FromResult<(IEnumerable<AIGuardrail>, int)>((items, all.Count));
    }

    /// <inheritdoc />
    public Task<IEnumerable<AIGuardrail>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idSet = ids.ToHashSet();
        var results = _guardrails.Values.Where(g => idSet.Contains(g.Id)).ToList();
        return Task.FromResult<IEnumerable<AIGuardrail>>(results);
    }

    /// <inheritdoc />
    public Task<AIGuardrail> SaveAsync(AIGuardrail guardrail, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var existing = _guardrails.GetValueOrDefault(guardrail.Id);
        if (existing is null)
        {
            guardrail.Version = 1;
            guardrail.CreatedByUserId = userId;
            guardrail.ModifiedByUserId = userId;
        }
        else
        {
            guardrail.Version = existing.Version + 1;
            guardrail.ModifiedByUserId = userId;
        }

        guardrail.DateModified = DateTime.UtcNow;
        _guardrails[guardrail.Id] = guardrail;
        return Task.FromResult(guardrail);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _guardrails.TryRemove(id, out _);
        return Task.FromResult(removed);
    }
}

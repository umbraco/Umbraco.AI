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
    public Task<IEnumerable<AIGuardrail>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idSet = ids.ToHashSet();
        var results = _guardrails.Values.Where(g => idSet.Contains(g.Id)).ToList();
        return Task.FromResult<IEnumerable<AIGuardrail>>(results);
    }

    /// <inheritdoc />
    public Task AddAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default)
    {
        _guardrails[guardrail.Id] = guardrail;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default)
    {
        _guardrails[guardrail.Id] = guardrail;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _guardrails.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _guardrails.Values.Any(g =>
            g.Alias.InvariantEquals(alias) && (!excludeId.HasValue || g.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Service for AI guardrail CRUD operations.
/// </summary>
internal sealed class AIGuardrailService : IAIGuardrailService
{
    private readonly IAIGuardrailRepository _repository;

    public AIGuardrailService(IAIGuardrailRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<AIGuardrail?> GetGuardrailAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<AIGuardrail?> GetGuardrailByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => await _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<AIGuardrail>> GetAllGuardrailsAsync(CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<AIGuardrail>> GetGuardrailsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        => await _repository.GetByIdsAsync(ids, cancellationToken);

    /// <inheritdoc />
    public async Task<AIGuardrail> CreateGuardrailAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default)
    {
        if (guardrail.Id == Guid.Empty)
        {
            guardrail.Id = Guid.NewGuid();
        }

        // Assign IDs to rules that don't have them
        foreach (var rule in guardrail.Rules)
        {
            if (rule.Id == Guid.Empty)
            {
                rule.Id = Guid.NewGuid();
            }
        }

        guardrail.DateModified = DateTime.UtcNow;
        guardrail.Version = 1;

        await _repository.AddAsync(guardrail, cancellationToken);
        return guardrail;
    }

    /// <inheritdoc />
    public async Task<AIGuardrail> UpdateGuardrailAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default)
    {
        // Assign IDs to new rules
        foreach (var rule in guardrail.Rules)
        {
            if (rule.Id == Guid.Empty)
            {
                rule.Id = Guid.NewGuid();
            }
        }

        guardrail.DateModified = DateTime.UtcNow;
        guardrail.Version++;

        await _repository.UpdateAsync(guardrail, cancellationToken);
        return guardrail;
    }

    /// <inheritdoc />
    public async Task DeleteGuardrailAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> GuardrailAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => await _repository.AliasExistsAsync(alias, excludeId, cancellationToken);
}

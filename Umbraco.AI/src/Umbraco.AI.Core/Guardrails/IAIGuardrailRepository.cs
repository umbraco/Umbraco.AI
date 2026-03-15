namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Repository interface for AI guardrail persistence.
/// </summary>
public interface IAIGuardrailRepository
{
    /// <summary>
    /// Gets a guardrail by its unique identifier.
    /// </summary>
    Task<AIGuardrail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a guardrail by its alias.
    /// </summary>
    Task<AIGuardrail?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all guardrails.
    /// </summary>
    Task<IEnumerable<AIGuardrail>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple guardrails by their IDs.
    /// </summary>
    Task<IEnumerable<AIGuardrail>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new guardrail.
    /// </summary>
    Task AddAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing guardrail.
    /// </summary>
    Task UpdateAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a guardrail by its ID.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a guardrail alias exists, optionally excluding a specific ID.
    /// </summary>
    Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

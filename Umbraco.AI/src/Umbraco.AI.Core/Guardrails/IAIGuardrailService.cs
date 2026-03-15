namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Service interface for AI guardrail CRUD operations.
/// </summary>
public interface IAIGuardrailService
{
    /// <summary>
    /// Gets a guardrail by its unique identifier.
    /// </summary>
    Task<AIGuardrail?> GetGuardrailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a guardrail by its alias.
    /// </summary>
    Task<AIGuardrail?> GetGuardrailByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all guardrails.
    /// </summary>
    Task<IEnumerable<AIGuardrail>> GetAllGuardrailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple guardrails by their IDs.
    /// </summary>
    Task<IEnumerable<AIGuardrail>> GetGuardrailsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new guardrail.
    /// </summary>
    Task<AIGuardrail> CreateGuardrailAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing guardrail.
    /// </summary>
    Task<AIGuardrail> UpdateGuardrailAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a guardrail by its ID.
    /// </summary>
    Task DeleteGuardrailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a guardrail alias exists, optionally excluding a specific ID.
    /// </summary>
    Task<bool> GuardrailAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

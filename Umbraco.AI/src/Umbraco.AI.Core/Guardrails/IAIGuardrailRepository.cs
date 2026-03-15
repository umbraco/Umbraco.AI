namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Defines a repository for managing AI guardrails.
/// Internal implementation detail - use <see cref="IAIGuardrailService"/> for external access.
/// </summary>
internal interface IAIGuardrailRepository
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
    /// Gets guardrails with pagination and optional filtering.
    /// </summary>
    Task<(IEnumerable<AIGuardrail> Items, int Total)> GetPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple guardrails by their IDs.
    /// </summary>
    Task<IEnumerable<AIGuardrail>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (creates or updates) a guardrail.
    /// </summary>
    Task<AIGuardrail> SaveAsync(AIGuardrail guardrail, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a guardrail by its ID.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

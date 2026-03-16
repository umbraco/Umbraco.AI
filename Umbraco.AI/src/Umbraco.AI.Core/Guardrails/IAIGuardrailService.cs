using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Defines a contract for managing AI guardrails.
/// </summary>
public interface IAIGuardrailService
{
    /// <summary>
    /// Gets a guardrail by its unique identifier.
    /// </summary>
    /// <param name="id">The guardrail ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guardrail if found, otherwise null.</returns>
    Task<AIGuardrail?> GetGuardrailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a guardrail by its alias.
    /// </summary>
    /// <param name="alias">The guardrail alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guardrail if found, otherwise null.</returns>
    Task<AIGuardrail?> GetGuardrailByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all guardrails.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All guardrails.</returns>
    Task<IEnumerable<AIGuardrail>> GetGuardrailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets guardrails with pagination and optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter to search by name or alias (case-insensitive contains).</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated guardrails and the total count.</returns>
    Task<(IEnumerable<AIGuardrail> Items, int Total)> GetGuardrailsPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple guardrails by their IDs.
    /// </summary>
    /// <param name="ids">The IDs to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guardrails matching the given IDs.</returns>
    Task<IEnumerable<AIGuardrail>> GetGuardrailsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (creates or updates) a guardrail.
    /// </summary>
    /// <param name="guardrail">The guardrail to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved guardrail.</returns>
    Task<AIGuardrail> SaveGuardrailAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a guardrail by its unique identifier.
    /// </summary>
    /// <param name="id">The guardrail ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteGuardrailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a guardrail with the given alias exists.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <param name="excludeId">Optional guardrail ID to exclude from the check (for editing existing guardrails).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a guardrail with the alias exists, false otherwise.</returns>
    Task<bool> GuardrailAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the version history for a guardrail with pagination.
    /// </summary>
    /// <param name="guardrailId">The guardrail ID.</param>
    /// <param name="skip">Number of versions to skip.</param>
    /// <param name="take">Maximum number of versions to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the paginated version history (ordered by version descending) and the total count.</returns>
    Task<(IEnumerable<AIEntityVersion> Items, int Total)> GetGuardrailVersionHistoryAsync(
        Guid guardrailId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version snapshot of a guardrail.
    /// </summary>
    /// <param name="guardrailId">The guardrail ID.</param>
    /// <param name="version">The version to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guardrail at that version, or null if not found.</returns>
    Task<AIGuardrail?> GetGuardrailVersionSnapshotAsync(
        Guid guardrailId,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a guardrail to a previous version.
    /// </summary>
    /// <param name="guardrailId">The guardrail ID.</param>
    /// <param name="targetVersion">The version to rollback to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated guardrail at the new version.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the guardrail or target version is not found.</exception>
    Task<AIGuardrail> RollbackGuardrailAsync(
        Guid guardrailId,
        int targetVersion,
        CancellationToken cancellationToken = default);
}

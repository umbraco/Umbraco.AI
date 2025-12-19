using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Repository interface for prompt persistence operations.
/// </summary>
public interface IAiAgentRepository
{
    /// <summary>
    /// Gets a prompt by its unique identifier.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found, null otherwise.</returns>
    Task<AiAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a prompt by its alias.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found, null otherwise.</returns>
    Task<AiAgent?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all Agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All Agents.</returns>
    Task<IEnumerable<AiAgent>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of Agents with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing Agents and total count.</returns>
    Task<PagedModel<AiAgent>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a AiAgent (creates or updates).
    /// </summary>
    /// <param name="AiAgent">The AiAgent to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved AiAgent.</returns>
    Task<AiAgent> SaveAsync(AiAgent AiAgent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a prompt by its ID.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a prompt with the given ID exists.
    /// </summary>
    /// <param name="id">The prompt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a prompt with the given alias exists.
    /// </summary>
    /// <param name="alias">The prompt alias.</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alias exists.</returns>
    Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Service interface for agent management operations.
/// </summary>
public interface IAiAgentService
{
    /// <summary>
    /// Gets a agent by its unique identifier.
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found, null otherwise.</returns>
    Task<AiAgent?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a agent by its alias.
    /// </summary>
    /// <param name="alias">The agent alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found, null otherwise.</returns>
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
    /// Save a agent (insert if new, update if exists) with validation.
    /// If agent.Id is Guid.Empty, a new Guid will be generated.
    /// </summary>
    /// <param name="agent">The agent to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved agent.</returns>
    Task<AiAgent> SaveAgentAsync(AiAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a agent.
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a agent with the given alias exists.
    /// </summary>
    /// <param name="alias">The agent alias.</param>
    /// <param name="excludeId">Optional ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alias exists.</returns>
    Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

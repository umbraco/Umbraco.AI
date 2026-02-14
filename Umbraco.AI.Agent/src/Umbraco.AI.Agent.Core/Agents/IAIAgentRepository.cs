using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Repository interface for agent persistence operations.
/// Internal implementation detail - use <see cref="IAIAgentService"/> for external access.
/// </summary>
internal interface IAIAgentRepository
{
    /// <summary>
    /// Gets a agent by its unique identifier.
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found, null otherwise.</returns>
    Task<AIAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a agent by its alias.
    /// </summary>
    /// <param name="alias">The agent alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found, null otherwise.</returns>
    Task<AIAgent?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all Agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All Agents.</returns>
    Task<IEnumerable<AIAgent>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of Agents with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="surfaceId">Optional surface ID filter.
    /// <param name="isActive">Optional active status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing Agents and total count.</returns>
    Task<PagedModel<AIAgent>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        string? surfaceId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all agents that belong to a specific surface.
    /// </summary>
    /// <param name="surfaceId">The surface ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agents that have the specified surface ID in their SurfaceIds.</returns>
    Task<IEnumerable<AIAgent>> GetBySurfaceAsync(string surfaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an agent (creates or updates).
    /// </summary>
    /// <param name="agent">The agent to save.</param>
    /// <param name="userId">Optional user key (GUID) for version tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved agent.</returns>
    Task<AIAgent> SaveAsync(AIAgent agent, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a agent by its ID.
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a agent with the given ID exists.
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a agent with the given alias exists.
    /// </summary>
    /// <param name="alias">The agent alias.</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alias exists.</returns>
    Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

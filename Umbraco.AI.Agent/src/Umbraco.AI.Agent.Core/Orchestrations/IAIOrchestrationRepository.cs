using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Repository interface for orchestration persistence operations.
/// Internal implementation detail - use <see cref="IAIOrchestrationService"/> for external access.
/// </summary>
internal interface IAIOrchestrationRepository
{
    /// <summary>
    /// Gets an orchestration by its unique identifier.
    /// </summary>
    /// <param name="id">The orchestration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The orchestration if found, null otherwise.</returns>
    Task<AIOrchestration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an orchestration by its alias.
    /// </summary>
    /// <param name="alias">The orchestration alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The orchestration if found, null otherwise.</returns>
    Task<AIOrchestration?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orchestrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All orchestrations.</returns>
    Task<IEnumerable<AIOrchestration>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of orchestrations with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="surfaceId">Optional surface ID filter.</param>
    /// <param name="isActive">Optional active status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing orchestrations and total count.</returns>
    Task<PagedModel<AIOrchestration>> GetPagedAsync(
        int skip,
        int take,
        string? filter = null,
        string? surfaceId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orchestrations that belong to a specific surface.
    /// </summary>
    /// <param name="surfaceId">The surface ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Orchestrations that have the specified surface ID in their SurfaceIds.</returns>
    Task<IEnumerable<AIOrchestration>> GetBySurfaceAsync(string surfaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an orchestration (creates or updates).
    /// </summary>
    /// <param name="orchestration">The orchestration to save.</param>
    /// <param name="userId">Optional user key (GUID) for version tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved orchestration.</returns>
    Task<AIOrchestration> SaveAsync(AIOrchestration orchestration, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an orchestration by its ID.
    /// </summary>
    /// <param name="id">The orchestration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an orchestration with the given ID exists.
    /// </summary>
    /// <param name="id">The orchestration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any orchestrations reference the specified profile.
    /// </summary>
    /// <param name="profileId">The profile ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if one or more orchestrations reference the profile.</returns>
    Task<bool> ExistsWithProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an orchestration with the given alias exists.
    /// </summary>
    /// <param name="alias">The orchestration alias.</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alias exists.</returns>
    Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

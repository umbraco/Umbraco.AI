using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Service interface for orchestration management operations.
/// </summary>
public interface IAIOrchestrationService
{
    /// <summary>
    /// Gets an orchestration by its unique identifier.
    /// </summary>
    /// <param name="id">The orchestration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The orchestration if found, null otherwise.</returns>
    Task<AIOrchestration?> GetOrchestrationAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an orchestration by its alias.
    /// </summary>
    /// <param name="alias">The orchestration alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The orchestration if found, null otherwise.</returns>
    Task<AIOrchestration?> GetOrchestrationByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orchestrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All orchestrations.</returns>
    Task<IEnumerable<AIOrchestration>> GetOrchestrationsAsync(CancellationToken cancellationToken = default);

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
    Task<PagedModel<AIOrchestration>> GetOrchestrationsPagedAsync(
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
    /// <returns>Orchestrations that have the specified surface ID.</returns>
    Task<IEnumerable<AIOrchestration>> GetOrchestrationsBySurfaceAsync(string surfaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save an orchestration (insert if new, update if exists) with validation.
    /// Validates graph integrity (exactly one Start node, reachability, valid agent references).
    /// If orchestration.Id is Guid.Empty, a new Guid will be generated.
    /// </summary>
    /// <param name="orchestration">The orchestration to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved orchestration.</returns>
    Task<AIOrchestration> SaveOrchestrationAsync(AIOrchestration orchestration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an orchestration.
    /// </summary>
    /// <param name="id">The orchestration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteOrchestrationAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an orchestration with the given alias exists.
    /// </summary>
    /// <param name="alias">The orchestration alias.</param>
    /// <param name="excludeId">Optional ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alias exists.</returns>
    Task<bool> OrchestrationAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether any orchestrations reference the specified profile.
    /// </summary>
    /// <param name="profileId">The profile ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if one or more orchestrations reference the profile, otherwise false.</returns>
    Task<bool> OrchestrationsExistWithProfileAsync(Guid profileId, CancellationToken cancellationToken = default);
}

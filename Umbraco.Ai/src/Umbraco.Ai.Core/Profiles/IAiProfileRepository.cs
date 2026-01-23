using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Profiles;

/// <summary>
/// Defines a repository for managing AI profiles.
/// Internal implementation detail - use <see cref="IAiProfileService"/> for external access.
/// </summary>
internal interface IAiProfileRepository
{
    /// <summary>
    /// Gets an AI profile by its unique identifier.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AiProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an AI profile by its alias.
    /// </summary>
    /// <param name="alias"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AiProfile?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all AI profiles.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<AiProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all AI profiles for a specific capability.
    /// </summary>
    /// <param name="capability"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<AiProfile>> GetByCapability(AiCapability capability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets AI profiles with pagination and optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter to search by name (case-insensitive contains).</param>
    /// <param name="capability">Optional capability to filter by.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated profiles and the total count.</returns>
    Task<(IEnumerable<AiProfile> Items, int Total)> GetPagedAsync(
        string? filter = null,
        AiCapability? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an AI profile.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    /// <param name="userId">Optional user ID for version tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved profile.</returns>
    Task<AiProfile> SaveAsync(AiProfile profile, int? userId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an AI profile by its unique identifier.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
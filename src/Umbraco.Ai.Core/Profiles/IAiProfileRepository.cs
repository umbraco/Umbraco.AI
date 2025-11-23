using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Profiles;

/// <summary>
/// Defines a repository for managing AI profiles.
/// </summary>
public interface IAiProfileRepository
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
    /// Saves an AI profile.
    /// </summary>
    /// <param name="profile"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AiProfile> SaveAsync(AiProfile profile, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an AI profile by its unique identifier.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
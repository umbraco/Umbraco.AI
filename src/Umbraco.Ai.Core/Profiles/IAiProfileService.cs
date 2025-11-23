using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Profiles;

/// <summary>
/// Defines a contract for resolving AI profiles based on capability and name.
/// </summary>
public interface IAiProfileService
{
    /// <summary>
    /// Gets a specific profile by name.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AiProfile?> GetProfileAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all profiles for the specified capability.
    /// </summary>
    /// <param name="capability"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<AiProfile>> GetProfilesAsync(AiCapability capability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default profile for the specified capability.
    /// </summary>
    /// <param name="capability"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AiProfile> GetDefaultProfileAsync(AiCapability capability, CancellationToken cancellationToken = default);
}
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Profiles;

/// <summary>
/// Defines a contract for managing AI profiles.
/// </summary>
public interface IAiProfileService
{
    /// <summary>
    /// Gets a specific profile by its unique identifier.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile if found, otherwise null.</returns>
    Task<AiProfile?> GetProfileAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific profile by its alias.
    /// </summary>
    /// <param name="alias">The profile alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile if found, otherwise null.</returns>
    Task<AiProfile?> GetProfileByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All profiles.</returns>
    Task<IEnumerable<AiProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all profiles for the specified capability.
    /// </summary>
    /// <param name="capability">The capability to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Profiles matching the capability.</returns>
    Task<IEnumerable<AiProfile>> GetProfilesAsync(AiCapability capability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default profile for the specified capability.
    /// </summary>
    /// <param name="capability">The capability.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default profile.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no default profile is configured or found.</exception>
    Task<AiProfile> GetDefaultProfileAsync(AiCapability capability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (creates or updates) a profile.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved profile.</returns>
    Task<AiProfile> SaveProfileAsync(AiProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a profile by its unique identifier.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteProfileAsync(Guid id, CancellationToken cancellationToken = default);
}
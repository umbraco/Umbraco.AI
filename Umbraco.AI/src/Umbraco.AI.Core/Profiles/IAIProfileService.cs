using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Defines a contract for managing AI profiles.
/// </summary>
public interface IAIProfileService
{
    /// <summary>
    /// Gets a specific profile by its unique identifier.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile if found, otherwise null.</returns>
    Task<AIProfile?> GetProfileAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific profile by its alias.
    /// </summary>
    /// <param name="alias">The profile alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile if found, otherwise null.</returns>
    Task<AIProfile?> GetProfileByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All profiles.</returns>
    Task<IEnumerable<AIProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all profiles for the specified capability.
    /// </summary>
    /// <param name="capability">The capability to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Profiles matching the capability.</returns>
    Task<IEnumerable<AIProfile>> GetProfilesAsync(AICapability capability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets profiles with pagination and optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter to search by name (case-insensitive contains).</param>
    /// <param name="capability">Optional capability to filter by.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated profiles and the total count.</returns>
    Task<(IEnumerable<AIProfile> Items, int Total)> GetProfilesPagedAsync(
        string? filter = null,
        AICapability? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default profile for the specified capability.
    /// </summary>
    /// <param name="capability">The capability.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default profile.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no default profile is configured or found.</exception>
    Task<AIProfile> GetDefaultProfileAsync(AICapability capability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (creates or updates) a profile.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved profile.</returns>
    Task<AIProfile> SaveProfileAsync(AIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a profile by its unique identifier.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteProfileAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the version history for a profile.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="skip">Number of versions to skip.</param>
    /// <param name="take">Maximum number of versions to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the paginated version history (ordered by version descending) and the total count.</returns>
    Task<(IEnumerable<AIEntityVersion> Items, int Total)> GetProfileVersionHistoryAsync(
        Guid profileId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version snapshot of a profile.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="version">The version to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile at that version, or null if not found.</returns>
    Task<AIProfile?> GetProfileVersionSnapshotAsync(
        Guid profileId,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a profile to a previous version.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="targetVersion">The version to rollback to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated profile at the new version.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the profile or target version is not found.</exception>
    Task<AIProfile> RollbackProfileAsync(
        Guid profileId,
        int targetVersion,
        CancellationToken cancellationToken = default);
}
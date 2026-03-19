using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IAIProfileService"/> to support alias-based lookups.
/// </summary>
public static class AIProfileServiceAliasExtensions
{
    /// <summary>
    /// Gets a profile ID by alias, throwing if not found.
    /// </summary>
    /// <param name="service">The profile service.</param>
    /// <param name="alias">The profile alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the profile is not found.</exception>
    public static async Task<Guid> GetProfileIdByAliasAsync(
        this IAIProfileService service,
        string alias,
        CancellationToken cancellationToken = default)
    {
        var profile = await service.GetProfileByAliasAsync(alias, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with alias '{alias}' not found.");
        }

        return profile.Id;
    }
}

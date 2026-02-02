using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IAiProfileService"/> to support IdOrAlias lookups.
/// </summary>
internal static class AIProfileServiceExtensions
{
    /// <summary>
    /// Gets a profile by ID or alias.
    /// </summary>
    /// <param name="service">The profile service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile if found, otherwise null.</returns>
    public static async Task<AIProfile?> GetProfileAsync(
        this IAiProfileService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetProfileAsync(idOrAlias.Id.Value, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetProfileByAliasAsync(idOrAlias.Alias, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Tries to get a profile ID by ID or alias.
    /// If already an ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The profile service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetProfileIdAsync(
        this IAiProfileService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        // If it's already an ID, return it directly (no DB lookup needed)
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return idOrAlias.Id.Value;
        }

        // For alias, we need to look up the profile
        if (idOrAlias.Alias != null)
        {
            var profile = await service.GetProfileByAliasAsync(idOrAlias.Alias, cancellationToken);
            return profile?.Id;
        }

        return null;
    }

    /// <summary>
    /// Gets a profile ID by ID or alias, throwing if not found.
    /// </summary>
    /// <param name="service">The profile service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the profile is not found.</exception>
    public static async Task<Guid> GetProfileIdAsync(
        this IAiProfileService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        var profileId = await TryGetProfileIdAsync(service, idOrAlias, cancellationToken);
        if (profileId is null)
        {
            throw new InvalidOperationException(
                $"Unable to find a profile with the id or alias of '{idOrAlias}'");
        }

        return profileId.Value;
    }
}

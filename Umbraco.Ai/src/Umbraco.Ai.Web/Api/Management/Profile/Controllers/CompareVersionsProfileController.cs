using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to compare two versions of a profile.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CompareVersionsProfileController : ProfileControllerBase
{
    private readonly IAiProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareVersionsProfileController"/> class.
    /// </summary>
    public CompareVersionsProfileController(IAiProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// Compare two versions of a profile.
    /// </summary>
    /// <param name="profileIdOrAlias">The unique identifier (GUID) or alias of the profile.</param>
    /// <param name="fromVersion">The source version to compare from.</param>
    /// <param name="toVersion">The target version to compare to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The differences between the two versions.</returns>
    [HttpGet($"{{{nameof(profileIdOrAlias)}}}/versions/compare")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(VersionComparisonResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareVersions(
        [FromRoute] IdOrAlias profileIdOrAlias,
        [FromQuery] int fromVersion,
        [FromQuery] int toVersion,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetProfileAsync(profileIdOrAlias, cancellationToken);
        if (profile is null)
        {
            return ProfileNotFound();
        }

        // Get the "from" version - this could be a historical snapshot or the current version
        var fromSnapshot = fromVersion == profile.Version
            ? profile
            : await _profileService.GetProfileVersionSnapshotAsync(profile.Id, fromVersion, cancellationToken);

        if (fromSnapshot is null)
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {fromVersion} was not found for this profile."));
        }

        // Get the "to" version - this could be a historical snapshot or the current version
        var toSnapshot = toVersion == profile.Version
            ? profile
            : await _profileService.GetProfileVersionSnapshotAsync(profile.Id, toVersion, cancellationToken);

        if (toSnapshot is null)
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {toVersion} was not found for this profile."));
        }

        var changes = CompareProfiles(fromSnapshot, toSnapshot);

        return Ok(new VersionComparisonResponseModel
        {
            FromVersion = fromVersion,
            ToVersion = toVersion,
            Changes = changes
        });
    }

    private static List<PropertyChangeModel> CompareProfiles(AiProfile from, AiProfile to)
    {
        var changes = new List<PropertyChangeModel>();

        // Compare Name
        if (from.Name != to.Name)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Name",
                OldValue = from.Name,
                NewValue = to.Name
            });
        }

        // Compare Alias
        if (from.Alias != to.Alias)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Alias",
                OldValue = from.Alias,
                NewValue = to.Alias
            });
        }

        // Compare ConnectionId
        if (from.ConnectionId != to.ConnectionId)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "ConnectionId",
                OldValue = from.ConnectionId?.ToString(),
                NewValue = to.ConnectionId?.ToString()
            });
        }

        // Compare Model
        var fromModel = from.Model?.ToString() ?? "";
        var toModel = to.Model?.ToString() ?? "";
        if (fromModel != toModel)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Model",
                OldValue = fromModel,
                NewValue = toModel
            });
        }

        // Compare Tags
        var fromTags = string.Join(", ", from.Tags ?? []);
        var toTags = string.Join(", ", to.Tags ?? []);
        if (fromTags != toTags)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Tags",
                OldValue = fromTags,
                NewValue = toTags
            });
        }

        // Compare Settings (as JSON)
        var fromSettings = from.Settings is not null ? JsonSerializer.Serialize(from.Settings) : "";
        var toSettings = to.Settings is not null ? JsonSerializer.Serialize(to.Settings) : "";
        if (fromSettings != toSettings)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Settings",
                OldValue = fromSettings,
                NewValue = toSettings
            });
        }

        return changes;
    }
}

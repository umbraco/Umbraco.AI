using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to get a specific version snapshot of a profile.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class VersionSnapshotProfileController : ProfileControllerBase
{
    private readonly IAiProfileService _profileService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionSnapshotProfileController"/> class.
    /// </summary>
    public VersionSnapshotProfileController(
        IAiProfileService profileService,
        IUmbracoMapper umbracoMapper)
    {
        _profileService = profileService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a specific version snapshot of a profile.
    /// </summary>
    /// <param name="profileIdOrAlias">The unique identifier (GUID) or alias of the profile.</param>
    /// <param name="version">The version number to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile at the specified version.</returns>
    [HttpGet($"{{{nameof(profileIdOrAlias)}}}/versions/{{{nameof(version)}:int}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProfileResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersionSnapshot(
        [FromRoute] IdOrAlias profileIdOrAlias,
        [FromRoute] int version,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetProfileAsync(profileIdOrAlias, cancellationToken);
        if (profile is null)
        {
            return ProfileNotFound();
        }

        var snapshot = await _profileService.GetProfileVersionSnapshotAsync(profile.Id, version, cancellationToken);
        if (snapshot is null)
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {version} was not found for this profile."));
        }

        return Ok(_umbracoMapper.Map<ProfileResponseModel>(snapshot));
    }
}

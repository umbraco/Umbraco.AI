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
/// Controller to rollback a profile to a previous version.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class RollbackProfileController : ProfileControllerBase
{
    private readonly IAiProfileService _profileService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RollbackProfileController"/> class.
    /// </summary>
    public RollbackProfileController(
        IAiProfileService profileService,
        IUmbracoMapper umbracoMapper)
    {
        _profileService = profileService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Rollback a profile to a previous version.
    /// </summary>
    /// <param name="profileIdOrAlias">The unique identifier (GUID) or alias of the profile.</param>
    /// <param name="version">The version number to rollback to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile at the new version (after rollback).</returns>
    [HttpPost($"{{{nameof(profileIdOrAlias)}}}/rollback/{{{nameof(version)}:int}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProfileResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RollbackToVersion(
        [FromRoute] IdOrAlias profileIdOrAlias,
        [FromRoute] int version,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetProfileAsync(profileIdOrAlias, cancellationToken);
        if (profile is null)
        {
            return ProfileNotFound();
        }

        try
        {
            var rolledBackProfile = await _profileService.RollbackProfileAsync(profile.Id, version, cancellationToken);
            return Ok(_umbracoMapper.Map<ProfileResponseModel>(rolledBackProfile));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Version"))
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {version} was not found for this profile."));
        }
    }
}

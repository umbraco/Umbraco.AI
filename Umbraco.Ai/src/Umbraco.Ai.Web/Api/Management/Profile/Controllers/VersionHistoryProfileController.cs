using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to get version history for a profile.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class VersionHistoryProfileController : ProfileControllerBase
{
    private readonly IAiProfileService _profileService;
    private readonly IUserService _userService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionHistoryProfileController"/> class.
    /// </summary>
    public VersionHistoryProfileController(
        IAiProfileService profileService,
        IUserService userService,
        IUmbracoMapper umbracoMapper)
    {
        _profileService = profileService;
        _userService = userService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get version history for a profile.
    /// </summary>
    /// <param name="profileIdOrAlias">The unique identifier (GUID) or alias of the profile.</param>
    /// <param name="skip">Number of versions to skip (for pagination).</param>
    /// <param name="take">Number of versions to return (for pagination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version history for the profile.</returns>
    [HttpGet($"{{{nameof(profileIdOrAlias)}}}/versions")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(EntityVersionHistoryResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersionHistory(
        [FromRoute] IdOrAlias profileIdOrAlias,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetProfileAsync(profileIdOrAlias, cancellationToken);
        if (profile is null)
        {
            return ProfileNotFound();
        }

        var versions = await _profileService.GetProfileVersionHistoryAsync(profile.Id, cancellationToken: cancellationToken);
        var versionList = versions.ToList();

        // Map to response models with user names
        var responseVersions = new List<EntityVersionResponseModel>();
        foreach (var version in versionList.Skip(skip).Take(take))
        {
            var responseModel = _umbracoMapper.Map<EntityVersionResponseModel>(version)!;

            // Resolve user name if we have a user ID
            if (version.CreatedByUserId.HasValue)
            {
                var user = await _userService.GetAsync(version.CreatedByUserId.Value);
                responseModel.CreatedByUserName = user?.Name;
            }

            responseVersions.Add(responseModel);
        }

        return Ok(new EntityVersionHistoryResponseModel
        {
            CurrentVersion = profile.Version,
            TotalVersions = versionList.Count,
            Versions = responseVersions
        });
    }
}

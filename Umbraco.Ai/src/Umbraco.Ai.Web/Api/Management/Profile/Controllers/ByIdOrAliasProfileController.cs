using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to get a profile by ID or alias.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdOrAliasProfileController : ProfileControllerBase
{
    private readonly IAiProfileService _profileService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdOrAliasProfileController"/> class.
    /// </summary>
    public ByIdOrAliasProfileController(IAiProfileService profileService, IUmbracoMapper umbracoMapper)
    {
        _profileService = profileService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a profile by its ID or alias.
    /// </summary>
    /// <param name="profileIdOrAlias">The unique identifier (GUID) or alias of the profile.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile details.</returns>
    [HttpGet($"{{{nameof(profileIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProfileResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileByIdOrAlias(
        [FromRoute] IdOrAlias profileIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetProfileAsync(profileIdOrAlias, cancellationToken);
        if (profile is null)
        {
            return ProfileNotFound();
        }

        return Ok(_umbracoMapper.Map<ProfileResponseModel>(profile));
    }
}

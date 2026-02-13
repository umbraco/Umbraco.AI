using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Profile.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to get a profile by ID or alias.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ByIdOrAliasProfileController : ProfileControllerBase
{
    private readonly IAIProfileService _profileService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdOrAliasProfileController"/> class.
    /// </summary>
    public ByIdOrAliasProfileController(IAIProfileService profileService, IUmbracoMapper umbracoMapper)
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

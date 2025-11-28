using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to get a profile by ID.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdProfileController : ProfileControllerBase
{
    private readonly IAiProfileRepository _profileRepository;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdProfileController"/> class.
    /// </summary>
    public ByIdProfileController(IAiProfileRepository profileRepository, IUmbracoMapper umbracoMapper)
    {
        _profileRepository = profileRepository;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a profile by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the profile.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile details.</returns>
    [HttpGet($"{{{nameof(id)}:guid}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProfileResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByIdAsync(id, cancellationToken);
        if (profile is null)
        {
            return ProfileNotFound();
        }

        return Ok(_umbracoMapper.Map<ProfileResponseModel>(profile));
    }
}

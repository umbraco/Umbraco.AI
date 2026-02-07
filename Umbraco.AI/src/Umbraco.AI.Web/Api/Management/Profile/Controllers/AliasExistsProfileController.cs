using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to check if a profile alias exists.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AliasExistsProfileController : ProfileControllerBase
{
    private readonly IAIProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AliasExistsProfileController"/> class.
    /// </summary>
    public AliasExistsProfileController(IAIProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// Checks if a profile with the given alias exists.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <param name="excludeId">Optional profile ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the alias exists, false otherwise.</returns>
    [HttpGet("{alias}/exists")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> AliasExists(
        string alias,
        [FromQuery] Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _profileService.ProfileAliasExistsAsync(alias, excludeId, cancellationToken);
        return Ok(exists);
    }
}

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to delete a profile.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteProfileController : ProfileControllerBase
{
    private readonly IAiProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteProfileController"/> class.
    /// </summary>
    public DeleteProfileController(IAiProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// Delete a profile.
    /// </summary>
    /// <param name="profileIdOrAlias">The unique identifier or alias of the profile to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete($"{{{nameof(profileIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfile(
        IdOrAlias profileIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        // Resolve to ID first since DeleteProfileAsync requires Guid
        var profileId = await _profileService.TryGetProfileIdAsync(profileIdOrAlias, cancellationToken);
        if (profileId is null)
        {
            return ProfileNotFound();
        }

        var deleted = await _profileService.DeleteProfileAsync(profileId.Value, cancellationToken);
        if (!deleted)
        {
            return ProfileNotFound();
        }

        return Ok();
    }
}

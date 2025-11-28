using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to delete a profile.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteProfileController : ProfileControllerBase
{
    private readonly IAiProfileRepository _profileRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteProfileController"/> class.
    /// </summary>
    public DeleteProfileController(IAiProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    /// <summary>
    /// Delete a profile.
    /// </summary>
    /// <param name="id">The unique identifier of the profile to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete($"{{{nameof(id)}:guid}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfileById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _profileRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return ProfileNotFound();
        }

        return Ok();
    }
}

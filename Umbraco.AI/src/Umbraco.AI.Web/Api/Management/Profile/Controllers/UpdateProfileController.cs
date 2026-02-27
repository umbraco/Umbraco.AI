using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Common.OperationStatus;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Profile.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to update an existing profile.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class UpdateProfileController : ProfileControllerBase
{
    private readonly IAIProfileService _profileService;
    private readonly IAIConnectionService _connectionService;
    private readonly AIProviderCollection _providers;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProfileController"/> class.
    /// </summary>
    public UpdateProfileController(
        IAIProfileService profileService,
        IAIConnectionService connectionService,
        AIProviderCollection providers,
        IUmbracoMapper umbracoMapper)
    {
        _profileService = profileService;
        _connectionService = connectionService;
        _providers = providers;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Update an existing profile.
    /// </summary>
    /// <param name="profileIdOrAlias">The unique identifier or alias of the profile to update.</param>
    /// <param name="requestModel">The updated profile data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut($"{{{nameof(profileIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        IdOrAlias profileIdOrAlias,
        UpdateProfileRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        var existing = await _profileService.GetProfileAsync(profileIdOrAlias, cancellationToken);
        if (existing is null)
        {
            return ProfileNotFound();
        }

        // Validate connection exists
        var connection = await _connectionService.GetConnectionAsync(requestModel.ConnectionId, cancellationToken);
        if (connection is null)
        {
            return ProfileOperationStatusResult(ProfileOperationStatus.ConnectionNotFound);
        }

        // Validate provider exists
        var provider = _providers.GetById(requestModel.Model.ProviderId);
        if (provider is null)
        {
            return ProfileOperationStatusResult(ProfileOperationStatus.ProviderNotFound);
        }

        AIProfile profile = _umbracoMapper.Map(requestModel, existing);
        await _profileService.SaveProfileAsync(profile, cancellationToken);
        return Ok();
    }
}

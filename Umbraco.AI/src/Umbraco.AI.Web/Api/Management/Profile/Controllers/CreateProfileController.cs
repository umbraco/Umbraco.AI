using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Management.Common.OperationStatus;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to create a new profile.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CreateProfileController : ProfileControllerBase
{
    private readonly IAiProfileService _profileService;
    private readonly IAiConnectionService _connectionService;
    private readonly AIProviderCollection _providers;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProfileController"/> class.
    /// </summary>
    public CreateProfileController(
        IAiProfileService profileService,
        IAiConnectionService connectionService,
        AIProviderCollection providers,
        IUmbracoMapper umbracoMapper)
    {
        _profileService = profileService;
        _connectionService = connectionService;
        _providers = providers;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Create a new profile.
    /// </summary>
    /// <param name="requestModel">The profile to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created profile ID.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProfile(
        CreateProfileRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        // Validate capability
        if (!Enum.TryParse<AICapability>(requestModel.Capability, true, out _))
        {
            return ProfileOperationStatusResult(ProfileOperationStatus.InvalidCapability);
        }

        // Check for duplicate alias
        var existingByAlias = await _profileService.GetProfileByAliasAsync(requestModel.Alias, cancellationToken);
        if (existingByAlias is not null)
        {
            return ProfileOperationStatusResult(ProfileOperationStatus.DuplicateAlias);
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

        AIProfile profile = _umbracoMapper.Map<AIProfile>(requestModel)!;
        var created = await _profileService.SaveProfileAsync(profile, cancellationToken);

        return CreatedAtAction(
            nameof(ByIdOrAliasProfileController.GetProfileByIdOrAlias),
            nameof(ByIdOrAliasProfileController).Replace("Controller", string.Empty),
            new { profileIdOrAlias = created.Id },
            created.Id.ToString());
    }
}

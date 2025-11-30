using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to create a new profile.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CreateProfileController : ProfileControllerBase
{
    private readonly IAiProfileRepository _profileRepository;
    private readonly IAiConnectionService _connectionService;
    private readonly AiProviderCollection _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProfileController"/> class.
    /// </summary>
    public CreateProfileController(
        IAiProfileRepository profileRepository,
        IAiConnectionService connectionService,
        AiProviderCollection providers)
    {
        _profileRepository = profileRepository;
        _connectionService = connectionService;
        _providers = providers;
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
        if (!Enum.TryParse<AiCapability>(requestModel.Capability, true, out var capability))
        {
            return ProfileOperationStatusResult(ProfileOperationStatus.InvalidCapability);
        }

        // Check for duplicate alias
        var existingByAlias = await _profileRepository.GetByAliasAsync(requestModel.Alias, cancellationToken);
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

        var profile = new AiProfile
        {
            Id = Guid.NewGuid(),
            Alias = requestModel.Alias,
            Name = requestModel.Name,
            Capability = capability,
            Model = new AiModelRef(requestModel.Model.ProviderId, requestModel.Model.ModelId),
            ConnectionId = requestModel.ConnectionId,
            Settings = MapSettingsFromRequest(capability, requestModel.Settings),
            Tags = requestModel.Tags
        };

        var created = await _profileRepository.SaveAsync(profile, cancellationToken);

        return CreatedAtAction(
            nameof(ByIdOrAliasProfileController.GetProfileByIdOrAlias),
            nameof(ByIdOrAliasProfileController).Replace("Controller", string.Empty),
            new { profileIdOrAlias = created.Id },
            created.Id.ToString());
    }

    private static IAiProfileSettings? MapSettingsFromRequest(AiCapability capability, ProfileSettingsModel? settings)
    {
        return capability switch
        {
            AiCapability.Chat when settings is ChatProfileSettingsModel chat => new AiChatProfileSettings
            {
                Temperature = chat.Temperature,
                MaxTokens = chat.MaxTokens,
                SystemPromptTemplate = chat.SystemPromptTemplate
            },
            AiCapability.Chat => new AiChatProfileSettings(), // Default empty chat settings
            AiCapability.Embedding => new AiEmbeddingProfileSettings(),
            _ => null
        };
    }
}

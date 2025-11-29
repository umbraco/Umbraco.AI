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
/// Controller to update an existing profile.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UpdateProfileController : ProfileControllerBase
{
    private readonly IAiProfileRepository _profileRepository;
    private readonly IAiConnectionService _connectionService;
    private readonly AiProviderCollection _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProfileController"/> class.
    /// </summary>
    public UpdateProfileController(
        IAiProfileRepository profileRepository,
        IAiConnectionService connectionService,
        AiProviderCollection providers)
    {
        _profileRepository = profileRepository;
        _connectionService = connectionService;
        _providers = providers;
    }

    /// <summary>
    /// Update an existing profile.
    /// </summary>
    /// <param name="id">The unique identifier of the profile to update.</param>
    /// <param name="requestModel">The updated profile data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut($"{{{nameof(id)}:guid}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfileById(
        Guid id,
        UpdateProfileRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        var existing = await _profileRepository.GetByIdAsync(id, cancellationToken);
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

        var profile = new AiProfile
        {
            Id = existing.Id,
            Alias = requestModel.Alias,
            Name = requestModel.Name,
            Capability = existing.Capability, // Capability cannot be changed after creation
            Model = new AiModelRef(requestModel.Model.ProviderId, requestModel.Model.ModelId),
            ConnectionId = requestModel.ConnectionId,
            Settings = MapSettingsFromRequest(existing.Capability, requestModel.Settings),
            Tags = requestModel.Tags
        };

        await _profileRepository.SaveAsync(profile, cancellationToken);
        return Ok();
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

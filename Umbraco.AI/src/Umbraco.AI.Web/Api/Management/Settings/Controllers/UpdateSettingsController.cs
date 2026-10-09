using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Settings.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Settings.Controllers;

/// <summary>
/// Controller to update the AI settings.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class UpdateSettingsController : SettingsControllerBase
{
    private readonly IAISettingsService _settingsService;
    private readonly IAIProfileService _profileService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSettingsController"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IAIProfileService so that default-profile slots are validated against their required capability. Will be removed in v20.")]
    public UpdateSettingsController(IAISettingsService settingsService, IUmbracoMapper umbracoMapper)
        : this(settingsService, StaticServiceProvider.Instance.GetRequiredService<IAIProfileService>(), umbracoMapper)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSettingsController"/> class.
    /// </summary>
    /// <remarks>
    /// Marked as the activation constructor: MVC builds controllers through
    /// <c>ActivatorUtilities</c>, which requires exactly one applicable constructor and throws when
    /// it can satisfy more than one. Keeping the obsolete overload around for binary compatibility
    /// means this attribute is what stops activation becoming ambiguous.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public UpdateSettingsController(
        IAISettingsService settingsService,
        IAIProfileService profileService,
        IUmbracoMapper umbracoMapper)
    {
        _settingsService = settingsService;
        _profileService = profileService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Update the AI settings.
    /// </summary>
    /// <param name="requestModel">The updated settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated settings.</returns>
    /// <remarks>
    /// Each default-profile slot (e.g. <see cref="UpdateSettingsRequestModel.DefaultChatProfileId"/>) is
    /// validated when set: the referenced profile must exist and must have the capability the slot
    /// requires. A null value always clears the slot without validation. Nothing is saved if any slot
    /// fails validation.
    /// </remarks>
    [HttpPut]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SettingsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings(
        UpdateSettingsRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        var settings = _umbracoMapper.Map<AISettings>(requestModel)!;

        (string SlotName, Guid? ProfileId, AICapability RequiredCapability)[] slots =
        [
            (nameof(AISettings.DefaultChatProfileId), settings.DefaultChatProfileId, AICapability.Chat),
            (nameof(AISettings.DefaultEmbeddingProfileId), settings.DefaultEmbeddingProfileId, AICapability.Embedding),
            (nameof(AISettings.DefaultSpeechToTextProfileId), settings.DefaultSpeechToTextProfileId, AICapability.SpeechToText),
            (nameof(AISettings.DefaultImageGenerationProfileId), settings.DefaultImageGenerationProfileId, AICapability.ImageGeneration),
            (nameof(AISettings.DefaultDecisionProfileId), settings.DefaultDecisionProfileId, AICapability.Decision),
            (nameof(AISettings.ClassifierChatProfileId), settings.ClassifierChatProfileId, AICapability.Chat),
        ];

        foreach (var slot in slots)
        {
            var error = await ValidateProfileSlotAsync(slot.SlotName, slot.ProfileId, slot.RequiredCapability, cancellationToken);
            if (error is not null)
            {
                return InvalidSettings(error);
            }
        }

        var saved = await _settingsService.SaveSettingsAsync(settings, cancellationToken);
        return Ok(_umbracoMapper.Map<SettingsResponseModel>(saved));
    }

    /// <summary>
    /// Validates that a default-profile slot, when set, references an existing profile with the
    /// capability the slot requires.
    /// </summary>
    /// <returns>A problem detail describing the failure, or null if the slot is valid (including unset).</returns>
    private async Task<string?> ValidateProfileSlotAsync(
        string slotName,
        Guid? profileId,
        AICapability requiredCapability,
        CancellationToken cancellationToken)
    {
        if (profileId is null)
        {
            return null;
        }

        var profile = await _profileService.GetProfileAsync(profileId.Value, cancellationToken);
        if (profile is null)
        {
            return $"{slotName} references a profile that does not exist.";
        }

        if (profile.Capability != requiredCapability)
        {
            return $"{slotName} must reference a profile with the {requiredCapability} capability, " +
                   $"but the specified profile has the {profile.Capability} capability.";
        }

        return null;
    }
}

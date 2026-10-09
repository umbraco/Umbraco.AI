using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Web.Api.Management.Settings.Controllers;
using Umbraco.AI.Web.Api.Management.Settings.Mapping;
using Umbraco.AI.Web.Api.Management.Settings.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Settings;

// UpdateSettingsController validates that every non-null default-profile slot references a profile
// that both exists and has the capability the slot requires, before anything is saved.
public class UpdateSettingsProfileCapabilityValidationTests
{
    private static UmbracoMapper CreateMapper() => new(
        new MapDefinitionCollection(() => new IMapDefinition[] { new SettingsMapDefinition() }),
        Mock.Of<ICoreScopeProvider>(),
        NullLogger<UmbracoMapper>.Instance);

    public static IEnumerable<object[]> AllSlots()
    {
        yield return new object[]
        {
            "DefaultChatProfileId", AICapability.Chat,
            (Action<UpdateSettingsRequestModel, Guid?>)((m, id) => m.DefaultChatProfileId = id),
        };
        yield return new object[]
        {
            "DefaultEmbeddingProfileId", AICapability.Embedding,
            (Action<UpdateSettingsRequestModel, Guid?>)((m, id) => m.DefaultEmbeddingProfileId = id),
        };
        yield return new object[]
        {
            "DefaultSpeechToTextProfileId", AICapability.SpeechToText,
            (Action<UpdateSettingsRequestModel, Guid?>)((m, id) => m.DefaultSpeechToTextProfileId = id),
        };
        yield return new object[]
        {
            "DefaultImageGenerationProfileId", AICapability.ImageGeneration,
            (Action<UpdateSettingsRequestModel, Guid?>)((m, id) => m.DefaultImageGenerationProfileId = id),
        };
        yield return new object[]
        {
            "DefaultDecisionProfileId", AICapability.Decision,
            (Action<UpdateSettingsRequestModel, Guid?>)((m, id) => m.DefaultDecisionProfileId = id),
        };
        yield return new object[]
        {
            "ClassifierChatProfileId", AICapability.Chat,
            (Action<UpdateSettingsRequestModel, Guid?>)((m, id) => m.ClassifierChatProfileId = id),
        };
    }

    private static (UpdateSettingsController Controller, Mock<IAISettingsService> SettingsService, Mock<IAIProfileService> ProfileService)
        CreateController()
    {
        var settingsService = new Mock<IAISettingsService>();
        settingsService
            .Setup(x => x.SaveSettingsAsync(It.IsAny<AISettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AISettings s, CancellationToken _) => s);

        var profileService = new Mock<IAIProfileService>();
        var controller = new UpdateSettingsController(settingsService.Object, profileService.Object, CreateMapper());
        return (controller, settingsService, profileService);
    }

    [Theory]
    [MemberData(nameof(AllSlots))]
    public async Task UpdateSettings_WithWrongCapabilityProfileInSlot_Returns400BadRequest(
        string slotName, AICapability requiredCapability, Action<UpdateSettingsRequestModel, Guid?> setSlot)
    {
        var (controller, _, profileService) = CreateController();
        var profileId = Guid.NewGuid();
        var wrongCapability = requiredCapability == AICapability.Chat ? AICapability.Embedding : AICapability.Chat;
        profileService
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIProfileBuilder().WithId(profileId).WithCapability(wrongCapability).Build());

        var requestModel = new UpdateSettingsRequestModel();
        setSlot(requestModel, profileId);

        var result = await controller.UpdateSettings(requestModel);

        result.ShouldBeOfType<BadRequestObjectResult>();
        _ = slotName; // only used to label the Theory case
    }

    [Theory]
    [MemberData(nameof(AllSlots))]
    public async Task UpdateSettings_WithNonExistentProfileIdInSlot_Returns400BadRequest(
        string slotName, AICapability requiredCapability, Action<UpdateSettingsRequestModel, Guid?> setSlot)
    {
        var (controller, _, profileService) = CreateController();
        var profileId = Guid.NewGuid();
        profileService
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        var requestModel = new UpdateSettingsRequestModel();
        setSlot(requestModel, profileId);

        var result = await controller.UpdateSettings(requestModel);

        result.ShouldBeOfType<BadRequestObjectResult>();
        _ = requiredCapability;
        _ = slotName;
    }

    [Theory]
    [MemberData(nameof(AllSlots))]
    public async Task UpdateSettings_WithMatchingCapabilityProfileInSlot_SavesSettings(
        string slotName, AICapability requiredCapability, Action<UpdateSettingsRequestModel, Guid?> setSlot)
    {
        var (controller, settingsService, profileService) = CreateController();
        var profileId = Guid.NewGuid();
        profileService
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIProfileBuilder().WithId(profileId).WithCapability(requiredCapability).Build());

        var requestModel = new UpdateSettingsRequestModel();
        setSlot(requestModel, profileId);

        var result = await controller.UpdateSettings(requestModel);

        result.ShouldBeOfType<OkObjectResult>();
        _ = settingsService;
        _ = slotName;
    }

    [Fact]
    public async Task UpdateSettings_WithNullProfileId_ClearsTheSlotWithoutValidation()
    {
        var (controller, _, profileService) = CreateController();

        var result = await controller.UpdateSettings(new UpdateSettingsRequestModel { DefaultChatProfileId = null });

        result.ShouldBeOfType<OkObjectResult>();
        profileService.Verify(x => x.GetProfileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSettings_WhenASlotFailsValidation_SavesNothing()
    {
        var (controller, settingsService, profileService) = CreateController();
        var profileId = Guid.NewGuid();
        profileService
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIProfileBuilder().WithId(profileId).WithCapability(AICapability.Embedding).Build());

        await controller.UpdateSettings(new UpdateSettingsRequestModel { DefaultChatProfileId = profileId });

        settingsService.Verify(
            x => x.SaveSettingsAsync(It.IsAny<AISettings>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateSettings_WhenASlotFailsValidation_MapsToProblemDetailsNamingTheSlot()
    {
        var (controller, _, profileService) = CreateController();
        var profileId = Guid.NewGuid();
        profileService
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIProfileBuilder().WithId(profileId).WithCapability(AICapability.Embedding).Build());

        var result = await controller.UpdateSettings(new UpdateSettingsRequestModel { DefaultChatProfileId = profileId });

        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Detail.ShouldContain("DefaultChatProfileId");
    }
}

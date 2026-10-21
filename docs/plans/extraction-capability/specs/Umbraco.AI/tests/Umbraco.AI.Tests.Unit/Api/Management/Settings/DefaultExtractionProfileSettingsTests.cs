// EX-4 — Set a default Extraction profile (AC1)
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental Extraction default-profile slot

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

// Copies DefaultDecisionProfileSettingsTests: real UpdateSettingsController / GetSettingsController + real
// SettingsMapDefinition; the settings service is the mocked collaborator. PUT echoes back what it was asked
// to save, and GET reads back what was saved, so the pair is the same round trip the Settings screen makes.
// When this moves into the test project, UpdateSettingsProfileCapabilityValidationTests.AllSlots also
// needs a "DefaultExtractionProfileId", AICapability.Extraction row.
public class DefaultExtractionProfileSettingsTests
{
    private static UmbracoMapper CreateMapper() => new(
        new MapDefinitionCollection(() => new IMapDefinition[] { new SettingsMapDefinition() }),
        Mock.Of<ICoreScopeProvider>(),
        NullLogger<UmbracoMapper>.Instance);

    #region Happy path

    public class GivenAnExtractionProfileIsPutAsTheDefault
    {
        private readonly Guid _profileId = Guid.NewGuid();
        private readonly Mock<IAISettingsService> _settingsService = new();
        private readonly Mock<IAIProfileService> _profileService = new();
        private readonly IActionResult _result;
        private AISettings? _saved;

        public GivenAnExtractionProfileIsPutAsTheDefault()
        {
            _settingsService
                .Setup(x => x.SaveSettingsAsync(It.IsAny<AISettings>(), It.IsAny<CancellationToken>()))
                .Callback<AISettings, CancellationToken>((s, _) => _saved = s)
                .ReturnsAsync((AISettings s, CancellationToken _) => s);

            _profileService
                .Setup(x => x.GetProfileAsync(_profileId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIProfileBuilder().WithId(_profileId).WithCapability(AICapability.Extraction).Build());

            _result = new UpdateSettingsController(_settingsService.Object, _profileService.Object, CreateMapper())
                .UpdateSettings(new UpdateSettingsRequestModel { DefaultExtractionProfileId = _profileId })
                .GetAwaiter().GetResult();
        }

        [Fact(Skip = "Pending")]
        public void SavesTheDefaultExtractionProfileId() => _saved!.DefaultExtractionProfileId.ShouldBe(_profileId);

        [Fact(Skip = "Pending")]
        public void ReturnsItInTheResponse()
            => ((SettingsResponseModel)((OkObjectResult)_result).Value!).DefaultExtractionProfileId.ShouldBe(_profileId);
    }

    public class GivenADefaultExtractionProfileIsSaved
    {
        private readonly Guid _profileId = Guid.NewGuid();
        private readonly IActionResult _result;

        public GivenADefaultExtractionProfileIsSaved()
        {
            var settingsService = new Mock<IAISettingsService>();
            settingsService
                .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AISettings { DefaultExtractionProfileId = _profileId });

            _result = new GetSettingsController(settingsService.Object, CreateMapper())
                .GetSettings().GetAwaiter().GetResult();
        }

        [Fact(Skip = "Pending")]
        public void GetSettingsReturnsIt()
            => ((SettingsResponseModel)((OkObjectResult)_result).Value!).DefaultExtractionProfileId.ShouldBe(_profileId);
    }

    #endregion

    #region Sad path

    public class GivenAChatProfileIsPutAsTheDefaultExtractionProfile
    {
        [Fact(Skip = "Pending")]
        public async Task ReturnsBadRequest()
        {
            var profileId = Guid.NewGuid();
            var profileService = new Mock<IAIProfileService>();
            profileService
                .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIProfileBuilder().WithId(profileId).WithCapability(AICapability.Chat).Build());

            var result = await new UpdateSettingsController(new Mock<IAISettingsService>().Object, profileService.Object, CreateMapper())
                .UpdateSettings(new UpdateSettingsRequestModel { DefaultExtractionProfileId = profileId });

            result.ShouldBeOfType<BadRequestObjectResult>();
        }
    }

    #endregion
}

// EX-1 — Extract typed values from C# (AC20, AC21, AC36): how the default Extraction profile is found.

using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// Exercises the real <see cref="AIProfileService"/>'s default-profile switch for
/// <see cref="AICapability.Extraction"/>, like <c>AIProfileServiceDefaultDecisionProfileTests</c>. Only its
/// repository/settings/options collaborators are mocked.
/// </summary>
public class DefaultExtractionProfileTests
{
    private static AIProfileService CreateService(
        Mock<IAIProfileRepository> repository,
        AISettings settings,
        AIOptions options)
    {
        var settingsService = new Mock<IAISettingsService>();
        settingsService.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var optionsMock = new Mock<IOptions<AIOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);

        var experimental = new Mock<IAIExperimentalFeatures>();
        experimental.Setup(x => x.IsCapabilityEnabled(It.IsAny<AICapability>())).Returns(true);

        return new AIProfileService(
            repository.Object,
            settingsService.Object,
            optionsMock.Object,
            new Mock<IAIEntityVersionService>().Object,
            new Mock<IEventAggregator>().Object,
            experimental.Object);
    }

    #region Happy path

    public class GivenAStoredDefaultExtractionProfileId
    {
        private readonly Guid _profileId = Guid.NewGuid();
        private readonly AIProfileService _service;

        public GivenAStoredDefaultExtractionProfileId()
        {
            var repository = new Mock<IAIProfileRepository>();
            repository
                .Setup(x => x.GetByIdAsync(_profileId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIProfileBuilder().WithId(_profileId).WithCapability(AICapability.Extraction).Build());

            _service = CreateService(
                repository,
                new AISettings { DefaultExtractionProfileId = _profileId },
                new AIOptions());
        }

        [Fact(Skip = "Pending")]
        public async Task ResolvesThatProfile()
        {
            var profile = await _service.GetDefaultProfileAsync(AICapability.Extraction);

            profile.Id.ShouldBe(_profileId);
        }
    }

    public class GivenOnlyAConfiguredDefaultExtractionProfileAlias
    {
        private readonly AIProfileService _service;

        public GivenOnlyAConfiguredDefaultExtractionProfileAlias()
        {
            var repository = new Mock<IAIProfileRepository>();
            repository
                .Setup(x => x.GetByAliasAsync("cloudflare-clairic", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIProfileBuilder()
                    .WithAlias("cloudflare-clairic")
                    .WithCapability(AICapability.Extraction)
                    .Build());

            _service = CreateService(
                repository,
                new AISettings(),
                new AIOptions { DefaultExtractionProfileAlias = "cloudflare-clairic" });
        }

        [Fact(Skip = "Pending")]
        public async Task ResolvesTheProfileWithThatAlias()
        {
            var profile = await _service.GetDefaultProfileAsync(AICapability.Extraction);

            profile.Alias.ShouldBe("cloudflare-clairic");
        }
    }

    #endregion

    #region Sad path

    public class GivenNoDefaultExtractionProfileStoredOrConfigured
    {
        private readonly AIProfileService _service = CreateService(
            new Mock<IAIProfileRepository>(),
            new AISettings(),
            new AIOptions());

        [Fact(Skip = "Pending")]
        public async Task ThrowsInvalidOperationException()
        {
            var act = () => _service.GetDefaultProfileAsync(AICapability.Extraction);

            await Should.ThrowAsync<InvalidOperationException>(act);
        }

        [Fact(Skip = "Pending")]
        public async Task SaysNoDefaultExtractionProfileIsSet()
        {
            var exception = await Record.ExceptionAsync(() => _service.GetDefaultProfileAsync(AICapability.Extraction));

            exception!.Message.ShouldContain("Default Extraction profile is not configured");
        }
    }

    #endregion
}

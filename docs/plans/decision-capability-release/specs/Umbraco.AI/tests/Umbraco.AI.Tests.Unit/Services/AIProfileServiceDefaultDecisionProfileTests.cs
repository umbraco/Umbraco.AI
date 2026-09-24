// DR-1 — Ask typed decisions from C# (default Decision profile resolution, AC6/AC7/AC12)

using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Tests.Unit.Services;

/// <summary>
/// Exercises the real <see cref="AIProfileService"/>'s default-profile switches for
/// <see cref="AICapability.Decision"/>. Only its repository/settings/options collaborators are mocked.
/// </summary>
public class AIProfileServiceDefaultDecisionProfileTests
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

    public class GivenAStoredDefaultDecisionProfileId
    {
        private readonly Guid _profileId = Guid.NewGuid();
        private readonly AIProfileService _service;

        public GivenAStoredDefaultDecisionProfileId()
        {
            var repository = new Mock<IAIProfileRepository>();
            repository
                .Setup(x => x.GetByIdAsync(_profileId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIProfileBuilder().WithId(_profileId).WithCapability(AICapability.Decision).Build());

            _service = CreateService(
                repository,
                new AISettings { DefaultDecisionProfileId = _profileId },
                new AIOptions());
        }

        [Fact(Skip = "Pending T3")]
        public async Task ResolvesThatProfile()
        {
            var profile = await _service.GetDefaultProfileAsync(AICapability.Decision);

            profile.Id.ShouldBe(_profileId);
        }
    }

    public class GivenOnlyAConfiguredDefaultDecisionProfileAlias
    {
        private readonly AIProfileService _service;

        public GivenOnlyAConfiguredDefaultDecisionProfileAlias()
        {
            var repository = new Mock<IAIProfileRepository>();
            repository
                .Setup(x => x.GetByAliasAsync("default-decision", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIProfileBuilder()
                    .WithAlias("default-decision")
                    .WithCapability(AICapability.Decision)
                    .Build());

            _service = CreateService(
                repository,
                new AISettings(),
                new AIOptions { DefaultDecisionProfileAlias = "default-decision" });
        }

        [Fact(Skip = "Pending T3")]
        public async Task ResolvesTheProfileWithThatAlias()
        {
            var profile = await _service.GetDefaultProfileAsync(AICapability.Decision);

            profile.Alias.ShouldBe("default-decision");
        }
    }

    // Sad path

    public class GivenNoDefaultDecisionProfileConfigured
    {
        private readonly AIProfileService _service = CreateService(
            new Mock<IAIProfileRepository>(),
            new AISettings(),
            new AIOptions());

        [Fact(Skip = "Pending T3")]
        public async Task ThrowsSayingNoDefaultDecisionProfileIsSet()
        {
            var exception = await Should.ThrowAsync<InvalidOperationException>(
                () => _service.GetDefaultProfileAsync(AICapability.Decision));

            exception.Message.ShouldContain("Default Decision profile is not configured");
        }
    }
}

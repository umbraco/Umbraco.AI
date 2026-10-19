// DR-8 — Deploy Decision setup between environments
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Xunit;

namespace Umbraco.AI.Deploy.Tests.Unit.Connectors.ServiceConnectors;

public class DecisionDeployTests
{
    private readonly Mock<IAISettingsService> _settingsServiceMock = new();
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly Mock<IAIConnectionService> _connectionServiceMock = new();
    private readonly Mock<UmbracoAIDeploySettingsAccessor> _settingsAccessorMock = new(MockBehavior.Strict, null!);
    private readonly UmbracoAISettingsServiceConnector _settingsConnector;
    private readonly UmbracoAIProfileServiceConnector _profileConnector;
    private readonly GuidUdi _settingsUdi = new(UmbracoAIConstants.UdiEntityType.Settings, AISettings.SettingsId);

    public DecisionDeployTests()
    {
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());
        _settingsConnector = new UmbracoAISettingsServiceConnector(
            _settingsServiceMock.Object, _profileServiceMock.Object, _settingsAccessorMock.Object);
        _profileConnector = new UmbracoAIProfileServiceConnector(
            _profileServiceMock.Object, _connectionServiceMock.Object, _settingsAccessorMock.Object);
    }

    #region Scenario: a default Decision profile is set and exported

    [Fact]
    public async Task Export_WithDecisionDefault_SetsDefaultDecisionProfileUdi()
    {
        var profileId = Guid.NewGuid();

        var artifact = await _settingsConnector.GetArtifactAsync(
            _settingsUdi, new AISettings { DefaultDecisionProfileId = profileId });

        artifact!.DefaultDecisionProfileUdi!.Guid.ShouldBe(profileId);
    }

    [Fact]
    public async Task Export_WithDecisionDefault_UsesProfileEntityType()
    {
        var artifact = await _settingsConnector.GetArtifactAsync(
            _settingsUdi, new AISettings { DefaultDecisionProfileId = Guid.NewGuid() });

        artifact!.DefaultDecisionProfileUdi!.EntityType.ShouldBe(UmbracoAIConstants.UdiEntityType.Profile);
    }

    [Fact]
    public async Task Export_WithDecisionDefault_AddsProfileDependency()
    {
        var profileId = Guid.NewGuid();

        var artifact = await _settingsConnector.GetArtifactAsync(
            _settingsUdi, new AISettings { DefaultDecisionProfileId = profileId });

        artifact!.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == UmbracoAIConstants.UdiEntityType.Profile &&
            ((GuidUdi)d.Udi).Guid == profileId);
    }

    #endregion

    #region Scenario: an artifact with a default Decision profile is imported

    [Fact]
    public async Task Import_WithDefaultDecisionProfileUdi_SetsDefaultDecisionProfileId()
    {
        var profileId = Guid.NewGuid();
        var settings = new AISettings();
        var resolvedProfile = new AIProfile
        {
            Alias = "decision-profile",
            Name = "Decision Profile",
            Capability = AICapability.Decision,
            Model = new AIModelRef("typesafe", "jev-latest"),
            ConnectionId = Guid.NewGuid()
        };
        _settingsServiceMock.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        _profileServiceMock.Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(resolvedProfile);
        AISettings? saved = null;
        _settingsServiceMock
            .Setup(x => x.SaveSettingsAsync(It.IsAny<AISettings>(), It.IsAny<CancellationToken>()))
            .Callback<AISettings, CancellationToken>((s, _) => saved = s)
            .ReturnsAsync((AISettings s, CancellationToken _) => s);
        var artifact = new AISettingsArtifact(_settingsUdi)
        {
            DefaultDecisionProfileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, profileId)
        };
        var state = new ArtifactDeployState<AISettingsArtifact, AISettings>(artifact, settings, _settingsConnector, 3);

        await _settingsConnector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);

        saved!.DefaultDecisionProfileId.ShouldBe(resolvedProfile.Id);
    }

    #endregion

    #region Scenario: a Decision profile artifact is imported

    [Fact]
    public async Task Import_DecisionProfile_RoundTripsNonNullDecisionSettings()
    {
        var connectionId = Guid.NewGuid();
        var profile = new AIProfile
        {
            Alias = "decision-profile",
            Name = "Decision Profile",
            Capability = AICapability.Decision,
            Model = new AIModelRef("typesafe", "jev-latest"),
            ConnectionId = connectionId,
            Settings = new AIDecisionProfileSettings()
        };
        var artifact = await _profileConnector.GetArtifactAsync(new GuidUdi("umbraco-ai-profile", profile.Id), profile);
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConnection { Alias = "conn", Name = "Conn", ProviderId = "typesafe" });
        AIProfile? saved = null;
        _profileServiceMock
            .Setup(x => x.SaveProfileAsync(It.IsAny<AIProfile>(), It.IsAny<CancellationToken>()))
            .Callback<AIProfile, CancellationToken>((p, _) => saved = p)
            .ReturnsAsync((AIProfile p, CancellationToken _) => p);
        var state = new ArtifactDeployState<AIProfileArtifact, AIProfile>(artifact!, null, _profileConnector, 2);

        await _profileConnector.ProcessAsync(state, Mock.Of<IDeployContext>(), 2);

        saved!.Settings.ShouldBeOfType<AIDecisionProfileSettings>();
    }

    #endregion

    #region Sad path: no default Decision profile

    [Fact]
    public async Task Export_WithoutDecisionDefault_LeavesUdiNull()
    {
        var artifact = await _settingsConnector.GetArtifactAsync(_settingsUdi, new AISettings());

        artifact!.DefaultDecisionProfileUdi.ShouldBeNull();
    }

    [Fact]
    public async Task Export_WithoutAnyDefaults_AddsNoProfileDependency()
    {
        var artifact = await _settingsConnector.GetArtifactAsync(_settingsUdi, new AISettings());

        artifact!.Dependencies.Count(d => d.Udi.EntityType == UmbracoAIConstants.UdiEntityType.Profile).ShouldBe(0);
    }

    #endregion
}

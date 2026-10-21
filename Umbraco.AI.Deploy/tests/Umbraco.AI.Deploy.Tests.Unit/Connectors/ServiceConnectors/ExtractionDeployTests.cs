// EX-12 — Deploy carries Extraction setup
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental Extraction profile settings and default slot

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

// Copies DecisionDeployTests. The Cloudflare connection itself needs nothing new: connections already
// deploy generically, with sensitive fields handled by the existing connection connector.
public class ExtractionDeployTests
{
    private readonly Mock<IAISettingsService> _settingsServiceMock = new();
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly Mock<IAIConnectionService> _connectionServiceMock = new();
    private readonly Mock<UmbracoAIDeploySettingsAccessor> _settingsAccessorMock = new(MockBehavior.Strict, null!);
    private readonly UmbracoAISettingsServiceConnector _settingsConnector;
    private readonly UmbracoAIProfileServiceConnector _profileConnector;
    private readonly GuidUdi _settingsUdi = new(UmbracoAIConstants.UdiEntityType.Settings, AISettings.SettingsId);

    public ExtractionDeployTests()
    {
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());
        _settingsConnector = new UmbracoAISettingsServiceConnector(
            _settingsServiceMock.Object, _profileServiceMock.Object, _settingsAccessorMock.Object);
        _profileConnector = new UmbracoAIProfileServiceConnector(
            _profileServiceMock.Object, _connectionServiceMock.Object, _settingsAccessorMock.Object);
    }

    private async Task<AIProfile?> ImportExtractionProfileAsync()
    {
        var connectionId = Guid.NewGuid();
        var profile = new AIProfile
        {
            Alias = "extraction-profile",
            Name = "Extraction Profile",
            Capability = AICapability.Extraction,
            Model = new AIModelRef("cloudflare", "@cf/cloudflare/clairic"),
            ConnectionId = connectionId,
            Settings = new AIExtractionProfileSettings()
        };
        var artifact = await _profileConnector.GetArtifactAsync(new GuidUdi("umbraco-ai-profile", profile.Id), profile);
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConnection { Alias = "conn", Name = "Conn", ProviderId = "cloudflare" });
        AIProfile? saved = null;
        _profileServiceMock
            .Setup(x => x.SaveProfileAsync(It.IsAny<AIProfile>(), It.IsAny<CancellationToken>()))
            .Callback<AIProfile, CancellationToken>((p, _) => saved = p)
            .ReturnsAsync((AIProfile p, CancellationToken _) => p);
        var state = new ArtifactDeployState<AIProfileArtifact, AIProfile>(artifact!, null, _profileConnector, 2);

        await _profileConnector.ProcessAsync(state, Mock.Of<IDeployContext>(), 2);

        return saved;
    }

    private async Task<AISettings?> ImportSettingsAsync(AISettingsArtifact artifact, AISettings settings)
    {
        _settingsServiceMock.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        AISettings? saved = null;
        _settingsServiceMock
            .Setup(x => x.SaveSettingsAsync(It.IsAny<AISettings>(), It.IsAny<CancellationToken>()))
            .Callback<AISettings, CancellationToken>((s, _) => saved = s)
            .ReturnsAsync((AISettings s, CancellationToken _) => s);
        var state = new ArtifactDeployState<AISettingsArtifact, AISettings>(artifact, settings, _settingsConnector, 3);

        await _settingsConnector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);

        return saved;
    }

    #region Scenario: an Extraction profile artifact is imported

    [Fact]
    public async Task Import_ExtractionProfile_KeepsTheExtractionCapability()
    {
        var saved = await ImportExtractionProfileAsync();

        saved!.Capability.ShouldBe(AICapability.Extraction);
    }

    [Fact]
    public async Task Import_ExtractionProfile_RoundTripsNonNullExtractionSettings()
    {
        var saved = await ImportExtractionProfileAsync();

        saved!.Settings.ShouldBeOfType<AIExtractionProfileSettings>();
    }

    #endregion

    #region Scenario: a default Extraction profile is set and exported

    [Fact]
    public async Task Export_WithExtractionDefault_SetsDefaultExtractionProfileUdi()
    {
        var profileId = Guid.NewGuid();

        var artifact = await _settingsConnector.GetArtifactAsync(
            _settingsUdi, new AISettings { DefaultExtractionProfileId = profileId });

        artifact!.DefaultExtractionProfileUdi!.Guid.ShouldBe(profileId);
    }

    [Fact]
    public async Task Export_WithExtractionDefault_UsesProfileEntityType()
    {
        var artifact = await _settingsConnector.GetArtifactAsync(
            _settingsUdi, new AISettings { DefaultExtractionProfileId = Guid.NewGuid() });

        artifact!.DefaultExtractionProfileUdi!.EntityType.ShouldBe(UmbracoAIConstants.UdiEntityType.Profile);
    }

    [Fact]
    public async Task Export_WithExtractionDefault_AddsProfileDependency()
    {
        var profileId = Guid.NewGuid();

        var artifact = await _settingsConnector.GetArtifactAsync(
            _settingsUdi, new AISettings { DefaultExtractionProfileId = profileId });

        artifact!.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == UmbracoAIConstants.UdiEntityType.Profile &&
            ((GuidUdi)d.Udi).Guid == profileId);
    }

    #endregion

    #region Scenario: an artifact with a default Extraction profile is imported

    [Fact]
    public async Task Import_WithDefaultExtractionProfileUdi_SetsDefaultExtractionProfileId()
    {
        var profileId = Guid.NewGuid();
        var resolvedProfile = new AIProfile
        {
            Alias = "extraction-profile",
            Name = "Extraction Profile",
            Capability = AICapability.Extraction,
            Model = new AIModelRef("cloudflare", "@cf/cloudflare/clairic"),
            ConnectionId = Guid.NewGuid()
        };
        _profileServiceMock.Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(resolvedProfile);
        var artifact = new AISettingsArtifact(_settingsUdi)
        {
            DefaultExtractionProfileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, profileId)
        };

        var saved = await ImportSettingsAsync(artifact, new AISettings());

        saved!.DefaultExtractionProfileId.ShouldBe(resolvedProfile.Id);
    }

    #endregion

    #region Sad path: no default Extraction profile

    [Fact]
    public async Task Export_WithoutExtractionDefault_LeavesUdiNull()
    {
        var artifact = await _settingsConnector.GetArtifactAsync(_settingsUdi, new AISettings());

        artifact!.DefaultExtractionProfileUdi.ShouldBeNull();
    }

    [Fact]
    public async Task Import_OlderArtifactWithoutExtractionUdi_Succeeds()
    {
        // An artifact exported before Extraction existed has no DefaultExtractionProfileUdi at all.
        var artifact = new AISettingsArtifact(_settingsUdi);

        await Should.NotThrowAsync(() => ImportSettingsAsync(artifact, new AISettings()));
    }

    [Fact]
    public async Task Import_OlderArtifactWithoutExtractionUdi_LeavesTheDefaultNull()
    {
        var artifact = new AISettingsArtifact(_settingsUdi);

        var saved = await ImportSettingsAsync(artifact, new AISettings());

        saved!.DefaultExtractionProfileId.ShouldBeNull();
    }

    #endregion
}

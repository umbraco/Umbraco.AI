using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
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

public class UmbracoAISettingsServiceConnectorTests
{
    private readonly Mock<IAISettingsService> _settingsServiceMock;
    private readonly Mock<IAIProfileService> _profileServiceMock;
    private readonly Mock<UmbracoAIDeploySettingsAccessor> _settingsAccessorMock;
    private readonly UmbracoAISettingsServiceConnector _connector;

    public UmbracoAISettingsServiceConnectorTests()
    {
        _settingsServiceMock = new Mock<IAISettingsService>();
        _profileServiceMock = new Mock<IAIProfileService>();
        _settingsAccessorMock = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);

        _settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());

        _connector = new UmbracoAISettingsServiceConnector(
            _settingsServiceMock.Object,
            _profileServiceMock.Object,
            _settingsAccessorMock.Object);
    }

    [Fact]
    public async Task GetArtifactAsync_WithImageGenerationDefault_AddsUdiAndDependency()
    {
        // Arrange
        var imageProfileId = Guid.NewGuid();
        var settings = new AISettings
        {
            DefaultImageGenerationProfileId = imageProfileId
        };

        var udi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Settings, AISettings.SettingsId);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, settings);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.DefaultImageGenerationProfileUdi.ShouldNotBeNull();
        artifact.DefaultImageGenerationProfileUdi.Guid.ShouldBe(imageProfileId);
        artifact.DefaultImageGenerationProfileUdi.EntityType.ShouldBe(UmbracoAIConstants.UdiEntityType.Profile);

        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == UmbracoAIConstants.UdiEntityType.Profile &&
            ((GuidUdi)d.Udi).Guid == imageProfileId);
    }

    [Fact]
    public async Task GetArtifactAsync_WithSpeechToTextDefault_AddsUdiAndDependency()
    {
        // Arrange
        var speechProfileId = Guid.NewGuid();
        var settings = new AISettings
        {
            DefaultSpeechToTextProfileId = speechProfileId
        };

        var udi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Settings, AISettings.SettingsId);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, settings);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.DefaultSpeechToTextProfileUdi.ShouldNotBeNull();
        artifact.DefaultSpeechToTextProfileUdi.Guid.ShouldBe(speechProfileId);
        artifact.DefaultSpeechToTextProfileUdi.EntityType.ShouldBe(UmbracoAIConstants.UdiEntityType.Profile);

        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == UmbracoAIConstants.UdiEntityType.Profile &&
            ((GuidUdi)d.Udi).Guid == speechProfileId);
    }

    [Fact]
    public async Task ProcessAsync_ResolvesSpeechToTextDefaultProfile()
    {
        // Arrange
        var speechProfileId = Guid.NewGuid();
        var settings = new AISettings();
        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var resolvedProfile = new AIProfile
        {
            Alias = "speech-profile",
            Name = "Speech Profile",
            Capability = AICapability.SpeechToText,
            Model = new AIModelRef("openai", "whisper-1"),
            ConnectionId = Guid.NewGuid()
        };
        _profileServiceMock
            .Setup(x => x.GetProfileAsync(speechProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedProfile);

        AISettings? savedSettings = null;
        _settingsServiceMock
            .Setup(x => x.SaveSettingsAsync(It.IsAny<AISettings>(), It.IsAny<CancellationToken>()))
            .Callback<AISettings, CancellationToken>((s, _) => savedSettings = s)
            .ReturnsAsync((AISettings s, CancellationToken _) => s);

        var udi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Settings, AISettings.SettingsId);
        var artifact = new AISettingsArtifact(udi)
        {
            DefaultSpeechToTextProfileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, speechProfileId)
        };

        var state = new ArtifactDeployState<AISettingsArtifact, AISettings>(
            artifact, settings, _connector, 3);

        // Act
        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);

        // Assert
        _profileServiceMock.Verify(
            x => x.GetProfileAsync(speechProfileId, It.IsAny<CancellationToken>()),
            Times.Once);
        savedSettings.ShouldNotBeNull();
        savedSettings.DefaultSpeechToTextProfileId.ShouldBe(resolvedProfile.Id);
    }

    [Fact]
    public async Task ProcessAsync_ResolvesImageGenerationDefaultProfile()
    {
        // Arrange
        var imageProfileId = Guid.NewGuid();
        var settings = new AISettings();
        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var resolvedProfile = new AIProfile
        {
            Alias = "image-profile",
            Name = "Image Profile",
            Capability = AICapability.ImageGeneration,
            Model = new AIModelRef("openai", "gpt-image-1"),
            ConnectionId = Guid.NewGuid()
        };
        _profileServiceMock
            .Setup(x => x.GetProfileAsync(imageProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedProfile);

        AISettings? savedSettings = null;
        _settingsServiceMock
            .Setup(x => x.SaveSettingsAsync(It.IsAny<AISettings>(), It.IsAny<CancellationToken>()))
            .Callback<AISettings, CancellationToken>((s, _) => savedSettings = s)
            .ReturnsAsync((AISettings s, CancellationToken _) => s);

        var udi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Settings, AISettings.SettingsId);
        var artifact = new AISettingsArtifact(udi)
        {
            DefaultImageGenerationProfileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, imageProfileId)
        };

        var state = new ArtifactDeployState<AISettingsArtifact, AISettings>(
            artifact, settings, _connector, 3);

        // Act
        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);

        // Assert
        _profileServiceMock.Verify(
            x => x.GetProfileAsync(imageProfileId, It.IsAny<CancellationToken>()),
            Times.Once);
        savedSettings.ShouldNotBeNull();
        savedSettings.DefaultImageGenerationProfileId.ShouldBe(resolvedProfile.Id);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Xunit;

namespace Umbraco.AI.Deploy.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAIProfileServiceConnectorTests
{
    private readonly Mock<IAIProfileService> _profileServiceMock;
    private readonly Mock<IAIConnectionService> _connectionServiceMock;
    private readonly Mock<UmbracoAIDeploySettingsAccessor> _settingsAccessorMock;
    private readonly UmbracoAIProfileServiceConnector _connector;

    public UmbracoAIProfileServiceConnectorTests()
    {
        _profileServiceMock = new Mock<IAIProfileService>();
        _connectionServiceMock = new Mock<IAIConnectionService>();
        _settingsAccessorMock = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);

        _settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());

        _connector = new UmbracoAIProfileServiceConnector(
            _profileServiceMock.Object,
            _connectionServiceMock.Object,
            _settingsAccessorMock.Object);
    }

    [Fact]
    public async Task GetArtifactAsync_CreatesArtifactWithConnectionDependency()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var profile = new AIProfile
        {
            Alias = "test-profile",
            Name = "Test Profile",
            Capability = AICapability.Chat,
            Model = new AIModelRef("openai", "gpt-4"),
            ConnectionId = connectionId,
            Tags = ["test", "profile"]
        };

        var udi = new GuidUdi("umbraco-ai-profile", profile.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, profile);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.Alias.ShouldBe("test-profile");
        artifact.Name.ShouldBe("Test Profile");
        artifact.Capability.ShouldBe((int)AICapability.Chat);
        artifact.ModelProviderId.ShouldBe("openai");
        artifact.ModelModelId.ShouldBe("gpt-4");
        artifact.Tags.ShouldBe(new[] { "test", "profile" });

        // Connection dependency should be added
        artifact.ConnectionUdi.ShouldNotBeNull();
        artifact.ConnectionUdi.Guid.ShouldBe(connectionId);
        artifact.ConnectionUdi.EntityType.ShouldBe("umbraco-ai-connection");

        // Dependency should be in the dependencies collection
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == "umbraco-ai-connection" &&
            ((GuidUdi)d.Udi).Guid == connectionId);
    }

    [Fact]
    public async Task GetArtifactAsync_WithNullProfile_ReturnsNull()
    {
        // Arrange
        var udi = new GuidUdi("umbraco-ai-profile", Guid.NewGuid());

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, null);

        // Assert
        artifact.ShouldBeNull();
    }

    [Fact]
    public async Task GetEntityAsync_ReturnsProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new AIProfile
        {
            Alias = "test-profile",
            Name = "Test Profile",
            Capability = AICapability.Chat,
            Model = new AIModelRef("openai", "gpt-4"),
            ConnectionId = Guid.NewGuid()
        };

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _connector.GetEntityAsync(profileId);

        // Assert
        result.ShouldBe(profile);
    }

    [Fact]
    public void GetEntityName_ReturnsProfileName()
    {
        // Arrange
        var profile = new AIProfile
        {
            Alias = "test-profile",
            Name = "Test Profile",
            Capability = AICapability.Chat,
            Model = new AIModelRef("openai", "gpt-4"),
            ConnectionId = Guid.NewGuid()
        };

        // Act
        var name = _connector.GetEntityName(profile);

        // Assert
        name.ShouldBe("Test Profile");
    }

    [Fact]
    public void UdiEntityType_ReturnsCorrectType()
    {
        // Assert
        _connector.UdiEntityType.ShouldBe("umbraco-ai-profile");
    }

    [Fact]
    public async Task ProcessAsync_ImageGenerationProfile_RoundTripsSettings()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var profile = new AIProfile
        {
            Alias = "image-profile",
            Name = "Image Profile",
            Capability = AICapability.ImageGeneration,
            Model = new AIModelRef("openai", "gpt-image-1"),
            ConnectionId = connectionId,
            Settings = new AIImageGenerationProfileSettings
            {
                Size = "1024x1024",
                Quality = "hd",
                Style = "vivid",
                MediaType = "image/png"
            }
        };

        var udi = new GuidUdi("umbraco-ai-profile", profile.Id);
        var artifact = await _connector.GetArtifactAsync(udi, profile);
        artifact.ShouldNotBeNull();

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConnection
            {
                Alias = "conn",
                Name = "Conn",
                ProviderId = "openai"
            });

        AIProfile? savedProfile = null;
        _profileServiceMock
            .Setup(x => x.SaveProfileAsync(It.IsAny<AIProfile>(), It.IsAny<CancellationToken>()))
            .Callback<AIProfile, CancellationToken>((p, _) => savedProfile = p)
            .ReturnsAsync((AIProfile p, CancellationToken _) => p);

        var state = new ArtifactDeployState<AIProfileArtifact, AIProfile>(
            artifact, null, _connector, 2);

        // Act
        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 2);

        // Assert
        savedProfile.ShouldNotBeNull();
        var settings = savedProfile.Settings.ShouldBeOfType<AIImageGenerationProfileSettings>();
        settings.Size.ShouldBe("1024x1024");
        settings.Quality.ShouldBe("hd");
        settings.Style.ShouldBe("vivid");
        settings.MediaType.ShouldBe("image/png");
    }

    [Fact]
    public async Task ProcessAsync_ChatProfile_RoundTripsSettings()
    {
        // Arrange - guards against the marker-interface being serialized as an empty object,
        // which would silently drop all concrete settings for every capability.
        var connectionId = Guid.NewGuid();
        var guardrailId = Guid.NewGuid();
        var profile = new AIProfile
        {
            Alias = "chat-profile",
            Name = "Chat Profile",
            Capability = AICapability.Chat,
            Model = new AIModelRef("openai", "gpt-4"),
            ConnectionId = connectionId,
            Settings = new AIChatProfileSettings
            {
                Temperature = 0.7f,
                MaxTokens = 1000,
                SystemPromptTemplate = "You are helpful.",
                GuardrailIds = [guardrailId]
            }
        };

        var udi = new GuidUdi("umbraco-ai-profile", profile.Id);
        var artifact = await _connector.GetArtifactAsync(udi, profile);
        artifact.ShouldNotBeNull();

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConnection
            {
                Alias = "conn",
                Name = "Conn",
                ProviderId = "openai"
            });

        AIProfile? savedProfile = null;
        _profileServiceMock
            .Setup(x => x.SaveProfileAsync(It.IsAny<AIProfile>(), It.IsAny<CancellationToken>()))
            .Callback<AIProfile, CancellationToken>((p, _) => savedProfile = p)
            .ReturnsAsync((AIProfile p, CancellationToken _) => p);

        var state = new ArtifactDeployState<AIProfileArtifact, AIProfile>(
            artifact, null, _connector, 2);

        // Act
        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 2);

        // Assert
        savedProfile.ShouldNotBeNull();
        var settings = savedProfile.Settings.ShouldBeOfType<AIChatProfileSettings>();
        settings.Temperature.ShouldBe(0.7f);
        settings.MaxTokens.ShouldBe(1000);
        settings.SystemPromptTemplate.ShouldBe("You are helpful.");
        settings.GuardrailIds.ShouldBe([guardrailId]);
    }
}

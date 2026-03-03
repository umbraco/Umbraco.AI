using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
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
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Xunit;

namespace Umbraco.AI.Deploy.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAIConnectionServiceConnectorTests
{
    private readonly Mock<IAIConnectionService> _connectionServiceMock;
    private readonly Mock<UmbracoAIDeploySettingsAccessor> _settingsAccessorMock;
    private readonly UmbracoAIConnectionServiceConnector _connector;

    public UmbracoAIConnectionServiceConnectorTests()
    {
        _connectionServiceMock = new Mock<IAIConnectionService>();
        _settingsAccessorMock = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);

        // Setup default settings
        var settings = new UmbracoAIDeploySettings
        {
            Connections = new UmbracoAIDeployConnectionSettings
            {
                IgnoreEncrypted = true,
                IgnoreSensitive = true,
                IgnoreSettings = []
            }
        };
        _settingsAccessorMock.Setup(x => x.Settings).Returns(settings);

        _connector = new UmbracoAIConnectionServiceConnector(
            _connectionServiceMock.Object,
            _settingsAccessorMock.Object);
    }

    [Fact]
    public async Task GetArtifactAsync_WithConfigurationReference_PreservesValue()
    {
        // Arrange
        var connection = new AIConnection
        {
            Alias = "test-connection",
            Name = "Test Connection",
            ProviderId = "test-provider",
            Settings = new Dictionary<string, object?>
            {
                ["ApiKey"] = "$OpenAI:ApiKey", // Configuration reference
                ["Endpoint"] = "https://api.example.com"
            }
        };

        var udi = new GuidUdi("umbraco-ai-connection", connection.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, connection);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();

        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        settings.ShouldNotBeNull();
        settings.ShouldContainKey("Endpoint");

        // Configuration reference should be preserved when IgnoreEncrypted is true
        // (only blocks ENC: values, allows $ refs)
        settings.ShouldContainKey("ApiKey");
        settings["ApiKey"]!.ToString().ShouldBe("$OpenAI:ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_WithEncryptedValue_FiltersWhenIgnoreEncryptedTrue()
    {
        // Arrange
        var connection = new AIConnection
        {
            Alias = "test-connection",
            Name = "Test Connection",
            ProviderId = "test-provider",
            Settings = new Dictionary<string, object?>
            {
                ["ApiKey"] = "ENC:abc123encrypted", // Encrypted value
                ["Endpoint"] = "https://api.example.com"
            }
        };

        var udi = new GuidUdi("umbraco-ai-connection", connection.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, connection);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();

        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        settings.ShouldNotBeNull();
        settings.ShouldContainKey("Endpoint");

        // Encrypted value should be filtered out when IgnoreEncrypted is true
        settings.ShouldNotContainKey("ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_WithIgnoreSettingsList_FiltersSpecifiedFields()
    {
        // Arrange
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings
        {
            Connections = new UmbracoAIDeployConnectionSettings
            {
                IgnoreEncrypted = false,
                IgnoreSensitive = false,
                IgnoreSettings = ["ApiKey"] // Specific field to ignore
            }
        });

        var connection = new AIConnection
        {
            Alias = "test-connection",
            Name = "Test Connection",
            ProviderId = "test-provider",
            Settings = new Dictionary<string, object?>
            {
                ["ApiKey"] = "secret-key",
                ["Endpoint"] = "https://api.example.com"
            }
        };

        var udi = new GuidUdi("umbraco-ai-connection", connection.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, connection);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();

        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        settings.ShouldNotBeNull();
        settings.ShouldContainKey("Endpoint");

        // ApiKey should be filtered (highest precedence - IgnoreSettings)
        settings.ShouldNotContainKey("ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_WithNullSettings_ReturnsNullSettings()
    {
        // Arrange
        var connection = new AIConnection
        {
            Alias = "test-connection",
            Name = "Test Connection",
            ProviderId = "test-provider",
            Settings = null
        };

        var udi = new GuidUdi("umbraco-ai-connection", connection.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, connection);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldBeNull();
    }

    [Fact]
    public async Task GetEntityAsync_ReturnsConnection()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AIConnection
        {
            Alias = "test-connection",
            Name = "Test Connection",
            ProviderId = "test-provider"
        };

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        // Act
        var result = await _connector.GetEntityAsync(connectionId);

        // Assert
        result.ShouldBe(connection);
    }

    [Fact]
    public void GetEntityName_ReturnsConnectionName()
    {
        // Arrange
        var connection = new AIConnection
        {
            Alias = "test-connection",
            Name = "Test Connection",
            ProviderId = "test-provider"
        };

        // Act
        var name = _connector.GetEntityName(connection);

        // Assert
        name.ShouldBe("Test Connection");
    }
}

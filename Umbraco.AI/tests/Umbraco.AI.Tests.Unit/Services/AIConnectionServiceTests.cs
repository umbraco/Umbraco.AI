using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Services;

public class AIConnectionServiceTests
{
    private readonly Mock<IAiConnectionRepository> _repositoryMock;
    private readonly Mock<IAiEditableModelResolver> _settingsResolverMock;
    private readonly Mock<IAiEntityVersionService> _versionServiceMock;

    public AIConnectionServiceTests()
    {
        _repositoryMock = new Mock<IAiConnectionRepository>();
        _settingsResolverMock = new Mock<IAiEditableModelResolver>();
        _versionServiceMock = new Mock<IAiEntityVersionService>();
    }

    private AIConnectionService CreateService(params IAiProvider[] providers)
    {
        var collection = new AIProviderCollection(() => providers);
        return new AIConnectionService(
            _repositoryMock.Object,
            collection,
            _settingsResolverMock.Object,
            _versionServiceMock.Object);
    }

    #region GetConnectionAsync

    [Fact]
    public async Task GetConnectionAsync_WithExistingId_ReturnsConnection()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithName("Test Connection")
            .WithProviderId("fake-provider")
            .Build();

        var service = CreateService();

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        // Act
        var result = await service.GetConnectionAsync(connectionId);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(connectionId);
        result.Name.ShouldBe("Test Connection");
    }

    [Fact]
    public async Task GetConnectionAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var service = CreateService();

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection?)null);

        // Act
        var result = await service.GetConnectionAsync(connectionId);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetConnectionsAsync

    [Fact]
    public async Task GetConnectionsAsync_WithNoFilter_ReturnsAllConnections()
    {
        // Arrange
        var connections = new List<AIConnection>
        {
            new AIConnectionBuilder().WithName("Connection 1").Build(),
            new AIConnectionBuilder().WithName("Connection 2").Build()
        };

        var service = CreateService();

        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connections);

        // Act
        var result = await service.GetConnectionsAsync();

        // Assert
        result.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetConnectionsAsync_WithProviderFilter_ReturnsFilteredConnections()
    {
        // Arrange
        var openAiConnections = new List<AIConnection>
        {
            new AIConnectionBuilder().WithName("OpenAI 1").WithProviderId("openai").Build(),
            new AIConnectionBuilder().WithName("OpenAI 2").WithProviderId("openai").Build()
        };

        var service = CreateService();

        _repositoryMock
            .Setup(x => x.GetByProviderAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(openAiConnections);

        // Act
        var result = await service.GetConnectionsAsync("openai");

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldAllBe(c => c.ProviderId == "openai");
    }

    [Fact]
    public async Task GetConnectionsAsync_WithEmptyProviderFilter_ReturnsAllConnections()
    {
        // Arrange
        var connections = new List<AIConnection>
        {
            new AIConnectionBuilder().WithName("Connection 1").Build()
        };

        var service = CreateService();

        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connections);

        // Act
        var result = await service.GetConnectionsAsync("");

        // Assert
        result.Count().ShouldBe(1);
        _repositoryMock.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetConnectionReferencesAsync

    [Fact]
    public async Task GetConnectionReferencesAsync_ReturnsReferencesForProvider()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connections = new List<AIConnection>
        {
            new AIConnectionBuilder()
                .WithId(connectionId)
                .WithName("OpenAI Connection")
                .WithProviderId("openai")
                .Build()
        };

        var service = CreateService();

        _repositoryMock
            .Setup(x => x.GetByProviderAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(connections);

        // Act
        var result = await service.GetConnectionReferencesAsync("openai");

        // Assert
        result.Count().ShouldBe(1);
        var reference = result.First();
        reference.Id.ShouldBe(connectionId);
        reference.Name.ShouldBe("OpenAI Connection");
    }

    #endregion

    #region SaveConnectionAsync

    [Fact]
    public async Task SaveConnectionAsync_WithNewConnection_GeneratesIdAndSaves()
    {
        // Arrange
        var connection = new AIConnectionBuilder()
            .WithId(Guid.Empty)
            .WithName("New Connection")
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
            .Build();

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var service = CreateService(fakeProvider);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, connection.Settings))
            .Returns((FakeProviderSettings?)connection.Settings);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIConnection>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection c, Guid? _, CancellationToken _) => c);

        // Act
        var result = await service.SaveConnectionAsync(connection);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe("New Connection");
        _repositoryMock.Verify(x => x.SaveAsync(It.Is<AIConnection>(c => c.Id != Guid.Empty), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveConnectionAsync_WithExistingConnection_PreservesIdAndSaves()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(existingId)
            .WithName("Existing Connection")
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
            .Build();

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var service = CreateService(fakeProvider);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, connection.Settings))
            .Returns((FakeProviderSettings?)connection.Settings);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIConnection>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection c, Guid? _, CancellationToken _) => c);

        // Act
        var result = await service.SaveConnectionAsync(connection);

        // Assert
        result.Id.ShouldBe(existingId);
        _repositoryMock.Verify(x => x.SaveAsync(It.Is<AIConnection>(c => c.Id == existingId), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveConnectionAsync_WithUnknownProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var connection = new AIConnectionBuilder()
            .WithProviderId("unknown-provider")
            .Build();

        var service = CreateService(); // Empty collection - no providers

        // Act
        var act = () => service.SaveConnectionAsync(connection);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Provider");
        exception.Message.ShouldContain("unknown-provider");
        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task SaveConnectionAsync_WithSettings_ValidatesSettings()
    {
        // Arrange
        var connection = new AIConnectionBuilder()
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
            .Build();

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var service = CreateService(fakeProvider);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, connection.Settings))
            .Returns((FakeProviderSettings?)connection.Settings);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIConnection>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection c, Guid? _, CancellationToken _) => c);

        // Act
        await service.SaveConnectionAsync(connection);

        // Assert - Settings resolver should be called to validate
        _settingsResolverMock.Verify(
            x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, connection.Settings),
            Times.Once);
    }

    [Fact]
    public async Task SaveConnectionAsync_UpdatesDateModified()
    {
        // Arrange
        var connection = new AIConnectionBuilder()
            .WithProviderId("fake-provider")
            .WithDateModified(DateTime.UtcNow.AddDays(-1))
            .Build();

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var service = CreateService(fakeProvider);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIConnection>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection c, Guid? _, CancellationToken _) => c);

        var beforeSave = DateTime.UtcNow;

        // Act
        var result = await service.SaveConnectionAsync(connection);

        // Assert
        result.DateModified.ShouldBeGreaterThanOrEqualTo(beforeSave);
    }

    #endregion

    #region DeleteConnectionAsync

    [Fact]
    public async Task DeleteConnectionAsync_WithExistingConnection_DeletesSuccessfully()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var service = CreateService();

        _repositoryMock
            .Setup(x => x.ExistsAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.DeleteAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await service.DeleteConnectionAsync(connectionId);

        // Assert
        _repositoryMock.Verify(x => x.DeleteAsync(connectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteConnectionAsync_WithNonExistingConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var service = CreateService();

        _repositoryMock
            .Setup(x => x.ExistsAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => service.DeleteConnectionAsync(connectionId);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"Connection with ID '{connectionId}' not found");
    }

    #endregion

    #region ValidateConnectionAsync

    [Fact]
    public async Task ValidateConnectionAsync_WithValidSettings_ReturnsTrue()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = "valid-key" };
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var service = CreateService(fakeProvider);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, settings))
            .Returns((FakeProviderSettings?)settings);

        // Act
        var result = await service.ValidateConnectionAsync("fake-provider", settings);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateConnectionAsync_WithUnknownProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService(); // Empty collection - no providers

        // Act
        var act = () => service.ValidateConnectionAsync("unknown-provider", new { });

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Provider");
        exception.Message.ShouldContain("unknown-provider");
        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ValidateConnectionAsync_WithInvalidSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new FakeProviderSettings { ApiKey = null };
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var service = CreateService(fakeProvider);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, settings))
            .Throws(new InvalidOperationException("Validation failed: API Key is required"));

        // Act
        var act = () => service.ValidateConnectionAsync("fake-provider", settings);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Failed to validate settings for provider");
    }

    #endregion

    #region TestConnectionAsync

    [Fact]
    public async Task TestConnectionAsync_WithValidConnection_ReturnsTrue()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var settings = new FakeProviderSettings { ApiKey = "test-key" };
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .WithSettings(settings)
            .Build();

        var fakeChatCapability = new FakeChatCapability();
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithChatCapability(fakeChatCapability);

        var service = CreateService(fakeProvider);

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, settings))
            .Returns((FakeProviderSettings?)settings);

        // Act
        var result = await service.TestConnectionAsync(connectionId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_WithConnectionNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var service = CreateService();

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection?)null);

        // Act
        var act = () => service.TestConnectionAsync(connectionId);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"Connection with ID '{connectionId}' not found");
    }

    [Fact]
    public async Task TestConnectionAsync_WithProviderNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("unknown-provider")
            .Build();

        var service = CreateService(); // Empty collection - no providers

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        // Act
        var act = () => service.TestConnectionAsync(connectionId);

        // Assert
        // GetConfiguredProviderAsync returns null when provider not found
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"Connection with ID '{connectionId}' not found or provider unavailable");
    }

    [Fact]
    public async Task TestConnectionAsync_WithProviderWithNoCapabilities_ThrowsInvalidOperationException()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var settings = new FakeProviderSettings { ApiKey = "test-key" };
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("empty-provider")
            .WithSettings(settings)
            .Build();

        var fakeProvider = new FakeAiProvider("empty-provider", "Empty Provider");
        // No capabilities added

        var service = CreateService(fakeProvider);

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, settings))
            .Returns((FakeProviderSettings?)settings);

        // Act
        var act = () => service.TestConnectionAsync(connectionId);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Provider");
        exception.Message.ShouldContain("empty-provider");
        exception.Message.ShouldContain("has no capabilities to test");
    }

    [Fact]
    public async Task TestConnectionAsync_WhenCapabilityThrows_ReturnsFalse()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var settings = new FakeProviderSettings { ApiKey = "invalid-key" };
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .WithSettings(settings)
            .Build();

        var failingCapability = new Mock<IAiChatCapability>();
        failingCapability
            .Setup(c => c.GetModelsAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Authentication failed"));
        failingCapability
            .Setup(c => c.Kind)
            .Returns(AICapability.Chat);

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithCapability(failingCapability.Object);

        var service = CreateService(fakeProvider);

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, settings))
            .Returns((FakeProviderSettings?)settings);

        // Act
        var result = await service.TestConnectionAsync(connectionId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GetConfiguredProviderAsync

    [Fact]
    public async Task GetConfiguredProviderAsync_WithValidConnection_ReturnsConfiguredProvider()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var settings = new FakeProviderSettings { ApiKey = "test-key" };
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .WithSettings(settings)
            .Build();

        var fakeChatCapability = new FakeChatCapability();
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithChatCapability(fakeChatCapability);

        var service = CreateService(fakeProvider);

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, settings))
            .Returns((FakeProviderSettings?)settings);

        // Act
        var result = await service.GetConfiguredProviderAsync(connectionId);

        // Assert
        result.ShouldNotBeNull();
        result!.Provider.ShouldBe(fakeProvider);
    }

    [Fact]
    public async Task GetConfiguredProviderAsync_WithConnectionNotFound_ReturnsNull()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var service = CreateService();

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConnection?)null);

        // Act
        var result = await service.GetConfiguredProviderAsync(connectionId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetConfiguredProviderAsync_WithProviderNotFound_ReturnsNull()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("unknown-provider")
            .Build();

        var service = CreateService(); // Empty collection - no providers

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        // Act
        var result = await service.GetConfiguredProviderAsync(connectionId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetConfiguredProviderAsync_WithSettingsResolutionFailed_ReturnsNull()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var settings = new FakeProviderSettings { ApiKey = "test-key" };
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .WithSettings(settings)
            .Build();

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var service = CreateService(fakeProvider);

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, settings))
            .Returns((FakeProviderSettings?)null); // Settings resolution failed

        // Act
        var result = await service.GetConfiguredProviderAsync(connectionId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetConfiguredProviderAsync_ReturnsConfiguredCapabilities()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var settings = new FakeProviderSettings { ApiKey = "test-key" };
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .WithSettings(settings)
            .Build();

        var fakeChatCapability = new FakeChatCapability();
        var fakeEmbeddingCapability = new FakeEmbeddingCapability();
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithChatCapability(fakeChatCapability)
            .WithEmbeddingCapability(fakeEmbeddingCapability);

        var service = CreateService(fakeProvider);

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, settings))
            .Returns((FakeProviderSettings?)settings);

        // Act
        var result = await service.GetConfiguredProviderAsync(connectionId);

        // Assert
        result.ShouldNotBeNull();
        result!.GetCapabilities().Count.ShouldBe(2);
        result.HasCapability<IAiConfiguredChatCapability>().ShouldBeTrue();
        result.HasCapability<IAiConfiguredEmbeddingCapability>().ShouldBeTrue();
        result.GetCapability<IAiConfiguredChatCapability>().ShouldNotBeNull();
        result.GetCapability<IAiConfiguredEmbeddingCapability>().ShouldNotBeNull();
    }

    [Fact]
    public async Task GetConfiguredProviderAsync_ConfiguredCapabilitiesHaveSettingsBakedIn()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var settings = new FakeProviderSettings { ApiKey = "test-key" };
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .WithSettings(settings)
            .Build();

        var fakeChatClient = new FakeChatClient();
        var fakeChatCapability = new FakeChatCapability(fakeChatClient);
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithChatCapability(fakeChatCapability);

        var service = CreateService(fakeProvider);

        _repositoryMock
            .Setup(x => x.GetAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _settingsResolverMock
            .Setup(x => x.ResolveModel<FakeProviderSettings>(fakeProvider.Id, settings))
            .Returns((FakeProviderSettings?)settings);

        // Act
        var result = await service.GetConfiguredProviderAsync(connectionId);
        var chatCapability = result!.GetCapability<IAiConfiguredChatCapability>();

        // Assert - CreateClient() has no settings parameter, settings are baked in
        var client = chatCapability!.CreateClient();
        client.ShouldBe(fakeChatClient);
    }

    #endregion
}

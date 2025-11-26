using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Factories;
using Umbraco.Ai.Core.Middleware;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Core.Settings;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Unit.Factories;

public class AiChatClientFactoryTests
{
    private readonly Mock<IAiRegistry> _registryMock;
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly Mock<IAiSettingsResolver> _settingsResolverMock;
    private readonly AiChatMiddlewareCollection _middleware;
    private readonly AiChatClientFactory _factory;

    public AiChatClientFactoryTests()
    {
        _registryMock = new Mock<IAiRegistry>();
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _settingsResolverMock = new Mock<IAiSettingsResolver>();
        _middleware = new AiChatMiddlewareCollection(() => Enumerable.Empty<IAiChatMiddleware>());

        _factory = new AiChatClientFactory(
            _registryMock.Object,
            _connectionServiceMock.Object,
            _settingsResolverMock.Object,
            _middleware);
    }

    #region CreateClientAsync - Valid profile and connection

    [Fact]
    public async Task CreateClientAsync_WithValidProfileAndConnection_ReturnsClient()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
            .IsActive(true)
            .Build();

        var profile = new AiProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "gpt-4")
            .WithCapability(AiCapability.Chat)
            .Build();

        var fakeChatClient = new FakeChatClient();
        var fakeChatCapability = new FakeChatCapability(fakeChatClient);
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithChatCapability(fakeChatCapability);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _registryMock
            .Setup(x => x.GetCapability<IAiChatCapability>("fake-provider"))
            .Returns(fakeChatCapability);

        _registryMock
            .Setup(x => x.GetProvider("fake-provider"))
            .Returns(fakeProvider);

        _settingsResolverMock
            .Setup(x => x.ResolveSettingsForProvider(fakeProvider, connection.Settings))
            .Returns(connection.Settings);

        // Act
        var client = await _factory.CreateClientAsync(profile);

        // Assert
        client.ShouldNotBeNull();
        client.ShouldBe(fakeChatClient);
    }

    #endregion

    #region CreateClientAsync - Empty connection ID

    [Fact]
    public async Task CreateClientAsync_WithEmptyConnectionId_ThrowsInvalidOperationException()
    {
        // Arrange
        var profile = new AiProfileBuilder()
            .WithConnectionId(Guid.Empty)
            .WithModel("fake-provider", "gpt-4")
            .WithName("Test Profile")
            .Build();

        // Act
        var act = () => _factory.CreateClientAsync(profile);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not specify a valid ConnectionId");
    }

    #endregion

    #region CreateClientAsync - Connection not found

    [Fact]
    public async Task CreateClientAsync_WithConnectionNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var profile = new AiProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "gpt-4")
            .WithName("Test Profile")
            .Build();

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConnection?)null);

        // Act
        var act = () => _factory.CreateClientAsync(profile);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"Connection with ID '{connectionId}' not found");
    }

    #endregion

    #region CreateClientAsync - Inactive connection

    [Fact]
    public async Task CreateClientAsync_WithInactiveConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithName("Inactive Connection")
            .WithProviderId("fake-provider")
            .IsActive(false)
            .Build();

        var profile = new AiProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "gpt-4")
            .WithName("Test Profile")
            .Build();

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        // Act
        var act = () => _factory.CreateClientAsync(profile);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("is not active");
    }

    #endregion

    #region CreateClientAsync - Provider mismatch

    [Fact]
    public async Task CreateClientAsync_WithProviderMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithName("OpenAI Connection")
            .WithProviderId("openai")
            .IsActive(true)
            .Build();

        var profile = new AiProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel("anthropic", "claude-3")
            .WithName("Anthropic Profile")
            .Build();

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        // Act
        var act = () => _factory.CreateClientAsync(profile);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Connection");
        exception.Message.ShouldContain("is for provider");
        exception.Message.ShouldContain("openai");
        exception.Message.ShouldContain("but profile");
        exception.Message.ShouldContain("requires provider");
        exception.Message.ShouldContain("anthropic");
    }

    #endregion

    #region CreateClientAsync - Provider not found

    [Fact]
    public async Task CreateClientAsync_WithProviderNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("unknown-provider")
            .IsActive(true)
            .Build();

        var profile = new AiProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel("unknown-provider", "some-model")
            .Build();

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _registryMock
            .Setup(x => x.GetProvider("unknown-provider"))
            .Returns((IAiProvider?)null);

        // Act
        var act = () => _factory.CreateClientAsync(profile);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Provider");
        exception.Message.ShouldContain("unknown-provider");
        exception.Message.ShouldContain("not found in registry");
    }

    #endregion

    #region CreateClientAsync - Provider lacks chat capability

    [Fact]
    public async Task CreateClientAsync_WithProviderLackingChatCapability_ThrowsInvalidOperationException()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("embedding-only-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
            .IsActive(true)
            .Build();

        var profile = new AiProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel("embedding-only-provider", "embed-model")
            .Build();

        var fakeProvider = new FakeAiProvider("embedding-only-provider", "Embedding Only Provider")
            .WithEmbeddingCapability(); // No chat capability

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _registryMock
            .Setup(x => x.GetProvider("embedding-only-provider"))
            .Returns(fakeProvider);

        _registryMock
            .Setup(x => x.GetCapability<IAiChatCapability>("embedding-only-provider"))
            .Returns((IAiChatCapability?)null);

        _settingsResolverMock
            .Setup(x => x.ResolveSettingsForProvider(fakeProvider, connection.Settings))
            .Returns(connection.Settings);

        // Act
        var act = () => _factory.CreateClientAsync(profile);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Provider");
        exception.Message.ShouldContain("embedding-only-provider");
        exception.Message.ShouldContain("does not support chat capability");
    }

    #endregion

    #region CreateClientAsync - Middleware applied

    [Fact]
    public async Task CreateClientAsync_WithMiddleware_AppliesMiddlewareInOrder()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
            .IsActive(true)
            .Build();

        var profile = new AiProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "gpt-4")
            .Build();

        var baseChatClient = new FakeChatClient();
        var wrappedClient1 = new FakeChatClient("Wrapped 1");
        var wrappedClient2 = new FakeChatClient("Wrapped 2");

        var middleware1Mock = new Mock<IAiChatMiddleware>();
        middleware1Mock.Setup(m => m.Apply(baseChatClient)).Returns(wrappedClient1);

        var middleware2Mock = new Mock<IAiChatMiddleware>();
        middleware2Mock.Setup(m => m.Apply(wrappedClient1)).Returns(wrappedClient2);

        var middlewareWithItems = new AiChatMiddlewareCollection(() => new[]
        {
            middleware1Mock.Object,
            middleware2Mock.Object
        });

        var factoryWithMiddleware = new AiChatClientFactory(
            _registryMock.Object,
            _connectionServiceMock.Object,
            _settingsResolverMock.Object,
            middlewareWithItems);

        var fakeChatCapability = new FakeChatCapability(baseChatClient);
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithChatCapability(fakeChatCapability);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _registryMock
            .Setup(x => x.GetCapability<IAiChatCapability>("fake-provider"))
            .Returns(fakeChatCapability);

        _registryMock
            .Setup(x => x.GetProvider("fake-provider"))
            .Returns(fakeProvider);

        _settingsResolverMock
            .Setup(x => x.ResolveSettingsForProvider(fakeProvider, connection.Settings))
            .Returns(connection.Settings);

        // Act
        var client = await factoryWithMiddleware.CreateClientAsync(profile);

        // Assert
        client.ShouldBe(wrappedClient2);
        middleware1Mock.Verify(m => m.Apply(baseChatClient), Times.Once);
        middleware2Mock.Verify(m => m.Apply(wrappedClient1), Times.Once);
    }

    [Fact]
    public async Task CreateClientAsync_WithEmptyMiddleware_ReturnsOriginalClient()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
            .IsActive(true)
            .Build();

        var profile = new AiProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "gpt-4")
            .Build();

        var baseChatClient = new FakeChatClient();
        var fakeChatCapability = new FakeChatCapability(baseChatClient);
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithChatCapability(fakeChatCapability);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _registryMock
            .Setup(x => x.GetCapability<IAiChatCapability>("fake-provider"))
            .Returns(fakeChatCapability);

        _registryMock
            .Setup(x => x.GetProvider("fake-provider"))
            .Returns(fakeProvider);

        _settingsResolverMock
            .Setup(x => x.ResolveSettingsForProvider(fakeProvider, connection.Settings))
            .Returns(connection.Settings);

        // Act
        var client = await _factory.CreateClientAsync(profile);

        // Assert
        client.ShouldBe(baseChatClient);
    }

    #endregion

    #region CreateClientAsync - Settings resolved before creating client

    [Fact]
    public async Task CreateClientAsync_ResolvesSettingsBeforeCreatingClient()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var rawSettings = new FakeProviderSettings { ApiKey = "$Config:ApiKey" };
        var resolvedSettings = new FakeProviderSettings { ApiKey = "resolved-api-key" };

        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .WithSettings(rawSettings)
            .IsActive(true)
            .Build();

        var profile = new AiProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient();
        var fakeChatCapability = new Mock<IAiChatCapability>();
        fakeChatCapability.Setup(c => c.CreateClient(resolvedSettings)).Returns(fakeChatClient);

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _registryMock
            .Setup(x => x.GetCapability<IAiChatCapability>("fake-provider"))
            .Returns(fakeChatCapability.Object);

        _registryMock
            .Setup(x => x.GetProvider("fake-provider"))
            .Returns(fakeProvider);

        _settingsResolverMock
            .Setup(x => x.ResolveSettingsForProvider(fakeProvider, rawSettings))
            .Returns(resolvedSettings);

        // Act
        var client = await _factory.CreateClientAsync(profile);

        // Assert
        client.ShouldBe(fakeChatClient);
        fakeChatCapability.Verify(c => c.CreateClient(resolvedSettings), Times.Once);
        _settingsResolverMock.Verify(x => x.ResolveSettingsForProvider(fakeProvider, rawSettings), Times.Once);
    }

    #endregion
}

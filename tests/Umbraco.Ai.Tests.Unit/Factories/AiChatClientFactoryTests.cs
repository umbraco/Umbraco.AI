using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Unit.Factories;

public class AiChatClientFactoryTests
{
    private readonly Mock<IAiConnectionService> _connectionServiceMock;
    private readonly AiChatMiddlewareCollection _middleware;

    public AiChatClientFactoryTests()
    {
        _connectionServiceMock = new Mock<IAiConnectionService>();
        _middleware = new AiChatMiddlewareCollection(() => Enumerable.Empty<IAiChatMiddleware>());
    }

    private AiChatClientFactory CreateFactory()
    {
        return new AiChatClientFactory(
            _connectionServiceMock.Object,
            _middleware);
    }

    private AiChatClientFactory CreateFactory(AiChatMiddlewareCollection middleware)
    {
        return new AiChatClientFactory(
            _connectionServiceMock.Object,
            middleware);
    }

    private static Mock<IConfiguredProvider> CreateConfiguredProviderMock(
        AiConnection connection,
        IAiProvider provider,
        IConfiguredChatCapability? chatCapability = null)
    {
        var mock = new Mock<IConfiguredProvider>();
        mock.Setup(x => x.Connection).Returns(connection);
        mock.Setup(x => x.Provider).Returns(provider);

        if (chatCapability is not null)
        {
            mock.Setup(x => x.GetCapability<IConfiguredChatCapability>()).Returns(chatCapability);
            mock.Setup(x => x.GetCapabilities()).Returns(new[] { chatCapability });
        }
        else
        {
            mock.Setup(x => x.GetCapability<IConfiguredChatCapability>()).Returns((IConfiguredChatCapability?)null);
            mock.Setup(x => x.GetCapabilities()).Returns(Array.Empty<IConfiguredCapability>());
        }

        return mock;
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
        var configuredCapabilityMock = new Mock<IConfiguredChatCapability>();
        configuredCapabilityMock.Setup(x => x.CreateClient()).Returns(fakeChatClient);
        configuredCapabilityMock.Setup(x => x.Kind).Returns(AiCapability.Chat);

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var configuredProviderMock = CreateConfiguredProviderMock(
            connection,
            fakeProvider,
            configuredCapabilityMock.Object);

        var factory = CreateFactory();

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        // Act
        var client = await factory.CreateClientAsync(profile);

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

        var factory = CreateFactory();

        // Act
        var act = () => factory.CreateClientAsync(profile);

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

        var factory = CreateFactory();

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IConfiguredProvider?)null);

        // Act
        var act = () => factory.CreateClientAsync(profile);

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

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var configuredProviderMock = CreateConfiguredProviderMock(connection, fakeProvider);

        var factory = CreateFactory();

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        // Act
        var act = () => factory.CreateClientAsync(profile);

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

        var fakeProvider = new FakeAiProvider("openai", "OpenAI"); // Connection's provider
        var configuredProviderMock = CreateConfiguredProviderMock(connection, fakeProvider);

        var factory = CreateFactory();

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        // Act
        var act = () => factory.CreateClientAsync(profile);

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

        var fakeProvider = new FakeAiProvider("embedding-only-provider", "Embedding Only Provider");
        // No chat capability - configured provider mock will return null for GetCapability<IConfiguredChatCapability>()
        var configuredProviderMock = CreateConfiguredProviderMock(connection, fakeProvider, chatCapability: null);

        var factory = CreateFactory();

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        // Act
        var act = () => factory.CreateClientAsync(profile);

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

        var configuredCapabilityMock = new Mock<IConfiguredChatCapability>();
        configuredCapabilityMock.Setup(x => x.CreateClient()).Returns(baseChatClient);
        configuredCapabilityMock.Setup(x => x.Kind).Returns(AiCapability.Chat);

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var configuredProviderMock = CreateConfiguredProviderMock(
            connection,
            fakeProvider,
            configuredCapabilityMock.Object);

        var factoryWithMiddleware = CreateFactory(middlewareWithItems);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

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
        var configuredCapabilityMock = new Mock<IConfiguredChatCapability>();
        configuredCapabilityMock.Setup(x => x.CreateClient()).Returns(baseChatClient);
        configuredCapabilityMock.Setup(x => x.Kind).Returns(AiCapability.Chat);

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var configuredProviderMock = CreateConfiguredProviderMock(
            connection,
            fakeProvider,
            configuredCapabilityMock.Object);

        var factory = CreateFactory();

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        // Act
        var client = await factory.CreateClientAsync(profile);

        // Assert
        client.ShouldBe(baseChatClient);
    }

    #endregion

    #region CreateClientAsync - Settings already resolved by configured provider

    [Fact]
    public async Task CreateClientAsync_UsesConfiguredProviderWithResolvedSettings()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId("fake-provider")
            .IsActive(true)
            .Build();

        var profile = new AiProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "gpt-4")
            .Build();

        var fakeChatClient = new FakeChatClient();
        var configuredCapabilityMock = new Mock<IConfiguredChatCapability>();
        configuredCapabilityMock.Setup(x => x.CreateClient()).Returns(fakeChatClient);
        configuredCapabilityMock.Setup(x => x.Kind).Returns(AiCapability.Chat);

        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider");
        var configuredProviderMock = CreateConfiguredProviderMock(
            connection,
            fakeProvider,
            configuredCapabilityMock.Object);

        var factory = CreateFactory();

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        // Act
        var client = await factory.CreateClientAsync(profile);

        // Assert
        client.ShouldBe(fakeChatClient);
        // Verify that CreateClient was called on the configured capability (no settings parameter)
        configuredCapabilityMock.Verify(c => c.CreateClient(), Times.Once);
        // Verify that GetConfiguredProviderAsync was called (which handles settings resolution)
        _connectionServiceMock.Verify(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}

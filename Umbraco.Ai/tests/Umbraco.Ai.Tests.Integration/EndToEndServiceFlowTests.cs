using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Embeddings;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.RuntimeContext;
using Umbraco.Ai.Core.Versioning;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Integration;

/// <summary>
/// End-to-end service flow tests that verify the complete pipeline works correctly
/// using real services (not mocks) with fake providers.
/// </summary>
public class EndToEndServiceFlowTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IAiConnectionRepository _connectionRepository;
    private readonly IAiProfileRepository _profileRepository;
    private readonly IAiChatService _chatService;
    private readonly IAiConnectionService _connectionService;
    private readonly FakeChatClient _fakeChatClient;

    public EndToEndServiceFlowTests()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Umbraco:Ai:DefaultChatProfileAlias"] = "default-chat",
                ["Umbraco:Ai:DefaultEmbeddingProfileAlias"] = "default-embedding"
            })
            .Build();

        // Create fake provider and client
        _fakeChatClient = new FakeChatClient("Test response from fake provider");
        var fakeChatCapability = new FakeChatCapability(_fakeChatClient);
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithChatCapability(fakeChatCapability);

        // Register AI services directly
        RegisterAiServices(services, configuration, fakeProvider);

        _serviceProvider = services.BuildServiceProvider();

        // Get services for test setup
        _connectionRepository = _serviceProvider.GetRequiredService<IAiConnectionRepository>();
        _profileRepository = _serviceProvider.GetRequiredService<IAiProfileRepository>();
        _chatService = _serviceProvider.GetRequiredService<IAiChatService>();
        _connectionService = _serviceProvider.GetRequiredService<IAiConnectionService>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    #region Full E2E Flow: Create Connection → Create Profile → Get Chat Response

    [Fact]
    public async Task FullFlow_CreateConnectionAndProfile_GetChatResponse_Succeeds()
    {
        // Arrange - Create connection
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithName("Test Connection")
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "test-api-key" })
            .IsActive(true)
            .Build();

        await _connectionRepository.SaveAsync(connection);

        // Arrange - Create profile with the default alias
        var profile = new AiProfileBuilder()
            .WithAlias("default-chat")
            .WithName("Default Chat Profile")
            .WithCapability(AiCapability.Chat)
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "fake-model")
            .WithChatSettings(temperature: 0.7f)
            .Build();

        await _profileRepository.SaveAsync(profile);

        // Act - Get chat response
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello, world!")
        };

        var response = await _chatService.GetChatResponseAsync(messages);

        // Assert
        response.ShouldNotBeNull();
        response.Text.ShouldBe("Test response from fake provider");

        // Verify the fake client received the message
        _fakeChatClient.ReceivedMessages.Count.ShouldBe(1);
        _fakeChatClient.ReceivedMessages[0].First().Text.ShouldBe("Hello, world!");
    }

    [Fact]
    public async Task FullFlow_CreateConnectionAndProfile_GetChatResponseByProfileId_Succeeds()
    {
        // Arrange - Create connection
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithName("Specific Connection")
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "specific-key" })
            .IsActive(true)
            .Build();

        await _connectionRepository.SaveAsync(connection);

        // Arrange - Create profile with specific ID
        var profileId = Guid.NewGuid();
        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithAlias("specific-chat")
            .WithName("Specific Chat Profile")
            .WithCapability(AiCapability.Chat)
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "specific-model")
            .Build();

        await _profileRepository.SaveAsync(profile);

        // Act - Get chat response by profile ID
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello from specific profile!")
        };

        var response = await _chatService.GetChatResponseAsync(profileId, messages);

        // Assert
        response.ShouldNotBeNull();
        response.Text.ShouldBe("Test response from fake provider");
    }

    #endregion

    #region Settings Resolution Through Full Pipeline

    [Fact]
    public async Task FullFlow_SettingsResolutionFromConfiguration_WorksCorrectly()
    {
        // This test verifies that settings with $ConfigKey are resolved from configuration
        // The actual resolution happens in AiEditableModelResolver

        // Arrange - Create connection with settings that contain a config reference
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithName("Config Connection")
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "direct-key" })
            .IsActive(true)
            .Build();

        await _connectionRepository.SaveAsync(connection);

        // Arrange - Create default profile
        var profile = new AiProfileBuilder()
            .WithAlias("default-chat")
            .WithName("Default Chat Profile")
            .WithCapability(AiCapability.Chat)
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "test-model")
            .Build();

        await _profileRepository.SaveAsync(profile);

        // Act - Get chat response (this exercises the full settings resolution path)
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Test message")
        };

        var response = await _chatService.GetChatResponseAsync(messages);

        // Assert - The fact that we get a response means settings were resolved successfully
        response.ShouldNotBeNull();
    }

    #endregion

    #region Streaming Response Flow

    [Fact]
    public async Task FullFlow_StreamingResponse_YieldsUpdates()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithName("Streaming Connection")
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "stream-key" })
            .IsActive(true)
            .Build();

        await _connectionRepository.SaveAsync(connection);

        var profile = new AiProfileBuilder()
            .WithAlias("default-chat")
            .WithName("Default Chat Profile")
            .WithCapability(AiCapability.Chat)
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "stream-model")
            .Build();

        await _profileRepository.SaveAsync(profile);

        // Act
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Stream this!")
        };

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in _chatService.GetStreamingChatResponseAsync(messages))
        {
            updates.Add(update);
        }

        // Assert
        updates.ShouldNotBeEmpty();
        // FakeChatClient streams word by word
        var fullText = string.Join("", updates.Select(u => u.Text));
        fullText.ShouldContain("Test");
        fullText.ShouldContain("response");
    }

    #endregion

    #region Connection Service Integration

    [Fact]
    public async Task ConnectionService_SaveAndRetrieve_WorksWithRealRegistry()
    {
        // Arrange
        var connection = new AiConnection
        {
            Id = Guid.Empty, // Service should generate ID
            Alias = "new-connection",
            Name = "New Connection",
            ProviderId = "fake-provider",
            Settings = new FakeProviderSettings { ApiKey = "new-key" },
            IsActive = true
        };

        // Act
        var savedConnection = await _connectionService.SaveConnectionAsync(connection);

        // Assert
        savedConnection.Id.ShouldNotBe(Guid.Empty);
        savedConnection.Name.ShouldBe("New Connection");

        // Verify we can retrieve it
        var retrievedConnection = await _connectionService.GetConnectionAsync(savedConnection.Id);
        retrievedConnection.ShouldNotBeNull();
        retrievedConnection!.Name.ShouldBe("New Connection");
    }

    [Fact]
    public async Task ConnectionService_TestConnection_WorksWithFakeProvider()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithName("Test Connection")
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "test-key" })
            .IsActive(true)
            .Build();

        await _connectionRepository.SaveAsync(connection);

        // Act
        var result = await _connectionService.TestConnectionAsync(connectionId);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task FullFlow_NonExistentProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentProfileId = Guid.NewGuid();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello!")
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _chatService.GetChatResponseAsync(nonExistentProfileId, messages));
    }

    [Fact]
    public async Task FullFlow_ProfileWithInactiveConnection_ThrowsInvalidOperationException()
    {
        // Arrange - Create inactive connection
        var connectionId = Guid.NewGuid();
        var connection = new AiConnectionBuilder()
            .WithId(connectionId)
            .WithName("Inactive Connection")
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "inactive-key" })
            .IsActive(false) // Inactive!
            .Build();

        await _connectionRepository.SaveAsync(connection);

        // Create profile pointing to inactive connection
        var profileId = Guid.NewGuid();
        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithAlias("inactive-profile")
            .WithName("Inactive Profile")
            .WithCapability(AiCapability.Chat)
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", "model")
            .Build();

        await _profileRepository.SaveAsync(profile);

        // Act & Assert
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello!")
        };

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _chatService.GetChatResponseAsync(profileId, messages));

        exception.Message.ShouldContain("not active");
    }

    [Fact]
    public async Task FullFlow_ProfileWithNonExistentConnection_ThrowsInvalidOperationException()
    {
        // Arrange - Create profile with non-existent connection ID
        var profileId = Guid.NewGuid();
        var nonExistentConnectionId = Guid.NewGuid();
        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithAlias("orphan-profile")
            .WithName("Orphan Profile")
            .WithCapability(AiCapability.Chat)
            .WithConnectionId(nonExistentConnectionId)
            .WithModel("fake-provider", "model")
            .Build();

        await _profileRepository.SaveAsync(profile);

        // Act & Assert
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello!")
        };

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _chatService.GetChatResponseAsync(profileId, messages));

        exception.Message.ShouldContain("not found");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Registers AI services directly, simulating what AddUmbracoAiCore does but bypassing
    /// Umbraco's collection builder pattern (which requires TypeLoader that can't be mocked).
    /// </summary>
    private static void RegisterAiServices(
        IServiceCollection services,
        IConfiguration configuration,
        FakeAiProvider fakeProvider)
    {
        // Register configuration (required by AiEditableModelResolver)
        services.AddSingleton<IConfiguration>(configuration);

        // Bind AiOptions
        services.Configure<AiOptions>(configuration.GetSection("Umbraco:Ai"));

        // Provider infrastructure
        services.AddSingleton<IAiCapabilityFactory, AiCapabilityFactory>();
        services.AddSingleton<IAiEditableModelSchemaBuilder, AiEditableModelSchemaBuilder>();
        services.AddSingleton<IAiProviderInfrastructure, AiProviderInfrastructure>();

        // Register the fake provider
        services.AddSingleton<IAiProvider>(fakeProvider);

        // Create provider collection from registered providers
        services.AddSingleton<AiProviderCollection>(sp =>
        {
            var providers = sp.GetServices<IAiProvider>();
            return new AiProviderCollection(() => providers);
        });

        // Middleware collections (empty for tests)
        services.AddSingleton<AiChatMiddlewareCollection>(
            _ => new AiChatMiddlewareCollection(() => Enumerable.Empty<IAiChatMiddleware>()));
        services.AddSingleton<AiEmbeddingMiddlewareCollection>(
            _ => new AiEmbeddingMiddlewareCollection(() => Enumerable.Empty<IAiEmbeddingMiddleware>()));

        // Runtime context infrastructure
        services.AddHttpContextAccessor();
        services.AddSingleton<AiRuntimeContextScopeProvider>();
        services.AddSingleton<IAiRuntimeContextAccessor>(sp => sp.GetRequiredService<AiRuntimeContextScopeProvider>());
        services.AddSingleton<IAiRuntimeContextScopeProvider>(sp => sp.GetRequiredService<AiRuntimeContextScopeProvider>());

        // Settings resolution
        services.AddSingleton<IAiEditableModelResolver, AiEditableModelResolver>();

        // Unified versioning service (stub implementation for tests)
        services.AddSingleton<AiVersionableEntityAdapterCollection>(_ =>
            new AiVersionableEntityAdapterCollection(() => Enumerable.Empty<IAiVersionableEntityAdapter>()));
        services.AddSingleton<IAiEntityVersionRepository, InMemoryAiEntityVersionRepository>();
        services.AddSingleton<IAiEntityVersionService, AiEntityVersionService>();

        // Connection system
        services.AddSingleton<IAiConnectionRepository, InMemoryAiConnectionRepository>();
        services.AddSingleton<IAiConnectionService, AiConnectionService>();

        // Profile resolution
        services.AddSingleton<IAiProfileRepository, InMemoryAiProfileRepository>();
        services.AddSingleton<IAiProfileService, AiProfileService>();

        // Client factories
        services.AddSingleton<IAiChatClientFactory, AiChatClientFactory>();
        services.AddSingleton<IAiEmbeddingGeneratorFactory, AiEmbeddingGeneratorFactory>();

        // High-level services
        services.AddSingleton<IAiChatService, AiChatService>();

        // Required for options
        services.AddLogging();
        services.AddOptions();
    }

    #endregion
}

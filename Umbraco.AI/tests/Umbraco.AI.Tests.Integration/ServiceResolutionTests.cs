using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.Cms.Core.Cache;

namespace Umbraco.AI.Tests.Integration;

/// <summary>
/// Targeted DI tests that verify our custom registration logic works correctly.
/// These are NOT tests that "DI works" - they test that our specific registrations
/// and provider discovery mechanism are correctly implemented.
/// </summary>
/// <remarks>
/// These tests use direct service registration (bypassing Umbraco's collection builder)
/// to verify that the core service graph can be resolved correctly.
/// </remarks>
public class ServiceResolutionTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public ServiceResolutionTests()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Umbraco:Ai:DefaultChatProfileAlias"] = "default-chat",
                ["Umbraco:Ai:DefaultEmbeddingProfileAlias"] = "default-embedding"
            })
            .Build();

        // Register AI services directly (simulating what AddUmbracoAiCore does)
        RegisterAiServices(services, configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    #region Smoke Test - Critical Service Graph

    [Fact]
    public void IAiChatService_CanBeResolved_WithEntireDependencyChain()
    {
        // This smoke test verifies the entire critical service graph resolves.
        // If any registration is missing, this will fail.
        var chatService = _serviceProvider.GetService<IAiChatService>();

        chatService.ShouldNotBeNull();
    }

    [Fact]
    public void AiProviderCollection_CanBeResolved()
    {
        var providers = _serviceProvider.GetService<AIProviderCollection>();

        providers.ShouldNotBeNull();
    }

    [Fact]
    public void IAiChatClientFactory_CanBeResolved()
    {
        var factory = _serviceProvider.GetService<IAiChatClientFactory>();

        factory.ShouldNotBeNull();
    }

    [Fact]
    public void IAiProfileService_CanBeResolved()
    {
        var service = _serviceProvider.GetService<IAiProfileService>();

        service.ShouldNotBeNull();
    }

    [Fact]
    public void IAiConnectionService_CanBeResolved()
    {
        var service = _serviceProvider.GetService<IAiConnectionService>();

        service.ShouldNotBeNull();
    }

    [Fact]
    public void IAiEditableModelResolver_CanBeResolved()
    {
        var resolver = _serviceProvider.GetService<IAiEditableModelResolver>();

        resolver.ShouldNotBeNull();
    }

    #endregion

    #region Infrastructure Services

    [Fact]
    public void IAiProviderInfrastructure_CanBeResolved()
    {
        var infrastructure = _serviceProvider.GetService<IAiProviderInfrastructure>();

        infrastructure.ShouldNotBeNull();
    }

    [Fact]
    public void IAiCapabilityFactory_CanBeResolved()
    {
        var factory = _serviceProvider.GetService<IAiCapabilityFactory>();

        factory.ShouldNotBeNull();
    }

    [Fact]
    public void IAiEditableModelSchemaBuilder_CanBeResolved()
    {
        var builder = _serviceProvider.GetService<IAiEditableModelSchemaBuilder>();

        builder.ShouldNotBeNull();
    }

    #endregion

    #region Repository Services

    [Fact]
    public void IAiConnectionRepository_CanBeResolved()
    {
        var repository = _serviceProvider.GetService<IAiConnectionRepository>();

        repository.ShouldNotBeNull();
    }

    [Fact]
    public void IAiProfileRepository_CanBeResolved()
    {
        var repository = _serviceProvider.GetService<IAiProfileRepository>();

        repository.ShouldNotBeNull();
    }

    #endregion

    #region Provider Collection Contains Fake Provider

    [Fact]
    public void AiProviderCollection_ContainsRegisteredProvider()
    {
        var providers = _serviceProvider.GetRequiredService<AIProviderCollection>();

        var provider = providers.GetById("fake-provider");

        provider.ShouldNotBeNull();
        provider!.Id.ShouldBe("fake-provider");
        provider.Name.ShouldBe("Fake Provider");
    }

    [Fact]
    public void AiProviderCollection_CanGetChatCapabilityFromProvider()
    {
        var providers = _serviceProvider.GetRequiredService<AIProviderCollection>();

        var capability = providers.GetCapability<IAiChatCapability>("fake-provider");

        capability.ShouldNotBeNull();
    }

    #endregion

    /// <summary>
    /// Registers AI services directly, simulating what AddUmbracoAiCore does but bypassing
    /// Umbraco's collection builder pattern (which requires TypeLoader that can't be mocked).
    /// </summary>
    private static void RegisterAiServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration (required by AIEditableModelResolver)
        services.AddSingleton<IConfiguration>(configuration);

        // Bind AIOptions
        services.Configure<AIOptions>(configuration.GetSection("Umbraco:Ai"));

        // Provider infrastructure
        services.AddSingleton<IAiCapabilityFactory, AICapabilityFactory>();
        services.AddSingleton<IAiEditableModelSchemaBuilder, AIEditableModelSchemaBuilder>();
        services.AddSingleton<IAiProviderInfrastructure, AIProviderInfrastructure>();

        // Register a fake provider (in real scenario, these are auto-discovered)
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithChatCapability()
            .WithEmbeddingCapability();
        services.AddSingleton<IAiProvider>(fakeProvider);

        // Create provider collection from registered providers
        services.AddSingleton<AIProviderCollection>(sp =>
        {
            var providers = sp.GetServices<IAiProvider>();
            return new AIProviderCollection(() => providers);
        });

        // Middleware collections (empty for tests)
        services.AddSingleton<AIChatMiddlewareCollection>(
            _ => new AIChatMiddlewareCollection(() => Enumerable.Empty<IAiChatMiddleware>()));
        services.AddSingleton<AIEmbeddingMiddlewareCollection>(
            _ => new AIEmbeddingMiddlewareCollection(() => Enumerable.Empty<IAiEmbeddingMiddleware>()));

        // Runtime context infrastructure
        services.AddHttpContextAccessor();
        services.AddSingleton<AIRuntimeContextScopeProvider>();
        services.AddSingleton<IAiRuntimeContextAccessor>(sp => sp.GetRequiredService<AIRuntimeContextScopeProvider>());
        services.AddSingleton<IAiRuntimeContextScopeProvider>(sp => sp.GetRequiredService<AIRuntimeContextScopeProvider>());

        // Runtime context contributors collection (empty for tests)
        services.AddSingleton<AIRuntimeContextContributorCollection>(
            _ => new AIRuntimeContextContributorCollection(() => Enumerable.Empty<IAiRuntimeContextContributor>()));

        // Settings resolution
        services.AddSingleton<IAiEditableModelResolver, AIEditableModelResolver>();

        // Settings service (required by AIProfileService)
        services.AddSingleton<IAppPolicyCache>(NoAppCache.Instance);
        services.AddSingleton<IAiSettingsRepository, InMemoryAiSettingsRepository>();
        services.AddSingleton<IAiSettingsService, AISettingsService>();

        // Unified versioning service (stub implementation for tests)
        services.AddSingleton<AIVersionableEntityAdapterCollection>(_ =>
            new AIVersionableEntityAdapterCollection(() => Enumerable.Empty<IAiVersionableEntityAdapter>()));
        services.AddSingleton<IAiEntityVersionRepository, InMemoryAiEntityVersionRepository>();
        services.AddSingleton<IAiEntityVersionService, AIEntityVersionService>();

        // Connection system
        services.AddSingleton<IAiConnectionRepository, InMemoryAiConnectionRepository>();
        services.AddSingleton<IAiConnectionService, AIConnectionService>();

        // Profile resolution
        services.AddSingleton<IAiProfileRepository, InMemoryAiProfileRepository>();
        services.AddSingleton<IAiProfileService, AIProfileService>();

        // Client factories
        services.AddSingleton<IAiChatClientFactory, AIChatClientFactory>();
        services.AddSingleton<IAiEmbeddingGeneratorFactory, AIEmbeddingGeneratorFactory>();

        // High-level services
        services.AddSingleton<IAiChatService, AIChatService>();

        // Required for options
        services.AddLogging();
        services.AddOptions();
    }
}

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
using Umbraco.Cms.Core.Events;

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
                ["Umbraco:AI:DefaultChatProfileAlias"] = "default-chat",
                ["Umbraco:AI:DefaultEmbeddingProfileAlias"] = "default-embedding"
            })
            .Build();

        // Register AI services directly (simulating what AddUmbracoAICore does)
        RegisterAIServices(services, configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    #region Smoke Test - Critical Service Graph

    [Fact]
    public void IAIChatService_CanBeResolved_WithEntireDependencyChain()
    {
        // This smoke test verifies the entire critical service graph resolves.
        // If any registration is missing, this will fail.
        var chatService = _serviceProvider.GetService<IAIChatService>();

        chatService.ShouldNotBeNull();
    }

    [Fact]
    public void AIProviderCollection_CanBeResolved()
    {
        var providers = _serviceProvider.GetService<AIProviderCollection>();

        providers.ShouldNotBeNull();
    }

    [Fact]
    public void IAIChatClientFactory_CanBeResolved()
    {
        var factory = _serviceProvider.GetService<IAIChatClientFactory>();

        factory.ShouldNotBeNull();
    }

    [Fact]
    public void IAIProfileService_CanBeResolved()
    {
        var service = _serviceProvider.GetService<IAIProfileService>();

        service.ShouldNotBeNull();
    }

    [Fact]
    public void IAIConnectionService_CanBeResolved()
    {
        var service = _serviceProvider.GetService<IAIConnectionService>();

        service.ShouldNotBeNull();
    }

    [Fact]
    public void IAIEditableModelResolver_CanBeResolved()
    {
        var resolver = _serviceProvider.GetService<IAIEditableModelResolver>();

        resolver.ShouldNotBeNull();
    }

    #endregion

    #region Infrastructure Services

    [Fact]
    public void IAIProviderInfrastructure_CanBeResolved()
    {
        var infrastructure = _serviceProvider.GetService<IAIProviderInfrastructure>();

        infrastructure.ShouldNotBeNull();
    }

    [Fact]
    public void IAICapabilityFactory_CanBeResolved()
    {
        var factory = _serviceProvider.GetService<IAICapabilityFactory>();

        factory.ShouldNotBeNull();
    }

    [Fact]
    public void IAIEditableModelSchemaBuilder_CanBeResolved()
    {
        var builder = _serviceProvider.GetService<IAIEditableModelSchemaBuilder>();

        builder.ShouldNotBeNull();
    }

    #endregion

    #region Repository Services

    [Fact]
    public void IAIConnectionRepository_CanBeResolved()
    {
        var repository = _serviceProvider.GetService<IAIConnectionRepository>();

        repository.ShouldNotBeNull();
    }

    [Fact]
    public void IAIProfileRepository_CanBeResolved()
    {
        var repository = _serviceProvider.GetService<IAIProfileRepository>();

        repository.ShouldNotBeNull();
    }

    #endregion

    #region Provider Collection Contains Fake Provider

    [Fact]
    public void AIProviderCollection_ContainsRegisteredProvider()
    {
        var providers = _serviceProvider.GetRequiredService<AIProviderCollection>();

        var provider = providers.GetById("fake-provider");

        provider.ShouldNotBeNull();
        provider!.Id.ShouldBe("fake-provider");
        provider.Name.ShouldBe("Fake Provider");
    }

    [Fact]
    public void AIProviderCollection_CanGetChatCapabilityFromProvider()
    {
        var providers = _serviceProvider.GetRequiredService<AIProviderCollection>();

        var capability = providers.GetCapability<IAIChatCapability>("fake-provider");

        capability.ShouldNotBeNull();
    }

    #endregion

    /// <summary>
    /// Registers AI services directly, simulating what AddUmbracoAICore does but bypassing
    /// Umbraco's collection builder pattern (which requires TypeLoader that can't be mocked).
    /// </summary>
    private static void RegisterAIServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration (required by AIEditableModelResolver)
        services.AddSingleton<IConfiguration>(configuration);

        // Bind AIOptions
        services.Configure<AIOptions>(configuration.GetSection("Umbraco:AI"));

        // Provider infrastructure
        services.AddSingleton<IAICapabilityFactory, AICapabilityFactory>();
        services.AddSingleton<IAIEditableModelSchemaBuilder, AIEditableModelSchemaBuilder>();
        services.AddSingleton<IAIProviderInfrastructure, AIProviderInfrastructure>();

        // Register a fake provider (in real scenario, these are auto-discovered)
        var fakeProvider = new FakeAIProvider("fake-provider", "Fake Provider")
            .WithChatCapability()
            .WithEmbeddingCapability();
        services.AddSingleton<IAIProvider>(fakeProvider);

        // Create provider collection from registered providers
        services.AddSingleton<AIProviderCollection>(sp =>
        {
            var providers = sp.GetServices<IAIProvider>();
            return new AIProviderCollection(() => providers);
        });

        // Middleware collections (empty for tests)
        services.AddSingleton<AIChatMiddlewareCollection>(
            _ => new AIChatMiddlewareCollection(() => Enumerable.Empty<IAIChatMiddleware>()));
        services.AddSingleton<AIEmbeddingMiddlewareCollection>(
            _ => new AIEmbeddingMiddlewareCollection(() => Enumerable.Empty<IAIEmbeddingMiddleware>()));

        // Runtime context infrastructure
        services.AddHttpContextAccessor();
        services.AddSingleton<AIRuntimeContextScopeProvider>();
        services.AddSingleton<IAIRuntimeContextAccessor>(sp => sp.GetRequiredService<AIRuntimeContextScopeProvider>());
        services.AddSingleton<IAIRuntimeContextScopeProvider>(sp => sp.GetRequiredService<AIRuntimeContextScopeProvider>());

        // Runtime context contributors collection (empty for tests)
        services.AddSingleton<AIRuntimeContextContributorCollection>(
            _ => new AIRuntimeContextContributorCollection(() => Enumerable.Empty<IAIRuntimeContextContributor>()));

        // Settings resolution
        services.AddSingleton<IAIEditableModelResolver, AIEditableModelResolver>();

        // Settings service (required by AIProfileService)
        services.AddSingleton<IAppPolicyCache>(NoAppCache.Instance);
        services.AddSingleton<IAISettingsRepository, InMemoryAISettingsRepository>();
        services.AddSingleton<IAISettingsService, AISettingsService>();

        // Event aggregator (required by services that publish notifications)
        services.AddSingleton(new Mock<IEventAggregator>().Object);

        // Unified versioning service (stub implementation for tests)
        services.AddSingleton<AIVersionableEntityAdapterCollection>(_ =>
            new AIVersionableEntityAdapterCollection(() => Enumerable.Empty<IAIVersionableEntityAdapter>()));
        services.AddSingleton<IAIEntityVersionRepository, InMemoryAIEntityVersionRepository>();
        services.AddSingleton<IAIEntityVersionService, AIEntityVersionService>();

        // Connection system
        services.AddSingleton<IAIConnectionRepository, InMemoryAIConnectionRepository>();
        services.AddSingleton<IAIConnectionService, AIConnectionService>();

        // Profile resolution
        services.AddSingleton<IAIProfileRepository, InMemoryAIProfileRepository>();
        services.AddSingleton<IAIProfileService, AIProfileService>();

        // Client factories
        services.AddSingleton<IAIChatClientFactory, AIChatClientFactory>();
        services.AddSingleton<IAIEmbeddingGeneratorFactory, AIEmbeddingGeneratorFactory>();

        // High-level services
        services.AddSingleton<IAIChatService, AIChatService>();

        // Required for options
        services.AddLogging();
        services.AddOptions();
    }
}

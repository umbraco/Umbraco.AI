using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Factories;
using Umbraco.Ai.Core.Middleware;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Core.Services;
using Umbraco.Ai.Core.Settings;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Integration;

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
    public void IAiRegistry_CanBeResolved()
    {
        var registry = _serviceProvider.GetService<IAiRegistry>();

        registry.ShouldNotBeNull();
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
    public void IAiSettingsResolver_CanBeResolved()
    {
        var resolver = _serviceProvider.GetService<IAiSettingsResolver>();

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
    public void IAiSettingDefinitionBuilder_CanBeResolved()
    {
        var builder = _serviceProvider.GetService<IAiSettingDefinitionBuilder>();

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

    #region Registry Contains Fake Provider

    [Fact]
    public void IAiRegistry_ContainsRegisteredProvider()
    {
        var registry = _serviceProvider.GetRequiredService<IAiRegistry>();

        var provider = registry.GetProvider("fake-provider");

        provider.ShouldNotBeNull();
        provider!.Id.ShouldBe("fake-provider");
        provider.Name.ShouldBe("Fake Provider");
    }

    [Fact]
    public void IAiRegistry_CanGetChatCapabilityFromProvider()
    {
        var registry = _serviceProvider.GetRequiredService<IAiRegistry>();

        var capability = registry.GetCapability<IAiChatCapability>("fake-provider");

        capability.ShouldNotBeNull();
    }

    #endregion

    /// <summary>
    /// Registers AI services directly, simulating what AddUmbracoAiCore does but bypassing
    /// Umbraco's collection builder pattern (which requires TypeLoader that can't be mocked).
    /// </summary>
    private static void RegisterAiServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration (required by AiSettingsResolver)
        services.AddSingleton<IConfiguration>(configuration);

        // Bind AiOptions
        services.Configure<AiOptions>(configuration.GetSection("Umbraco:Ai"));

        // Provider infrastructure
        services.AddSingleton<IAiCapabilityFactory, AiCapabilityFactory>();
        services.AddSingleton<IAiSettingDefinitionBuilder, AiSettingDefinitionBuilder>();
        services.AddSingleton<IAiProviderInfrastructure, AiProviderInfrastructure>();

        // Register a fake provider (in real scenario, these are auto-discovered)
        var fakeProvider = new FakeAiProvider("fake-provider", "Fake Provider")
            .WithChatCapability()
            .WithEmbeddingCapability();
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

        // Registry
        services.AddSingleton<IAiRegistry, AiRegistry>();

        // Settings resolution
        services.AddSingleton<IAiSettingsResolver, AiSettingsResolver>();

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
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.SpeechToText;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;

#pragma warning disable MEAI001 // ISpeechToTextClient / IImageGenerator are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Exercises the experimental image-generation tracking middleware

namespace Umbraco.AI.Tests.Integration.Observability;

/// <summary>
/// DI-resolution guard test for the tracker-backed middleware introduced by the #195 observability
/// refactor (chat/embedding/speech-to-text/image-generation each collapsed onto a single
/// <c>AITracking*Middleware</c> backed by the internal <see cref="IAIOperationTracker"/> singleton).
/// </summary>
/// <remarks>
/// A prior production bug (#224) shipped an <c>internal</c> constructor on one of these tracking
/// middleware classes. Unit tests did not catch it because they construct clients (and middleware)
/// directly rather than resolving the pipeline through a real <see cref="IServiceProvider"/> — and
/// the default Microsoft.Extensions.DependencyInjection container only ever considers a type's
/// <em>public</em> constructors (mirroring Umbraco's <c>CollectionBuilderBase</c>, which registers
/// each middleware type as itself and resolves it via <c>factory.GetRequiredService(itemType)</c>).
/// This test builds the DI container the same way the other integration DI-resolution tests in this
/// project do (bypassing Umbraco's TypeLoader-based collection builder, which cannot run outside a
/// full CMS host — see <c>ServiceResolutionTests</c> / <c>EndToEndServiceFlowTests</c> for precedent),
/// but registers each tracking middleware as a real singleton service exactly as
/// <c>CollectionBuilderBase.RegisterTypes</c> would, then resolves the four public client factories
/// and drives each through <c>CreateClientAsync</c>/<c>CreateGeneratorAsync</c> against a fake
/// provider — the same code path that applies the middleware pipeline in production. If any tracking
/// middleware's constructor becomes non-public (or a dependency of <see cref="IAIOperationTracker"/>
/// stops resolving), the underlying <see cref="IServiceProvider"/> throws and these tests fail.
/// </remarks>
public class AIOperationTrackerDiResolutionTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IAIConnectionRepository _connectionRepository;
    private readonly IAIProfileRepository _profileRepository;

    public AIOperationTrackerDiResolutionTests()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Umbraco:AI:DefaultChatProfileAlias"] = "default-chat",
                ["Umbraco:AI:DefaultEmbeddingProfileAlias"] = "default-embedding"
            })
            .Build();

        var fakeProvider = new FakeAIProvider("fake-provider", "Fake Provider")
            .WithChatCapability()
            .WithEmbeddingCapability()
            .WithSpeechToTextCapability()
            .WithCapability<IAIImageGeneratorCapability>(new FakeImageGeneratorCapability());

        RegisterAIServices(services, configuration, fakeProvider);

        _serviceProvider = services.BuildServiceProvider();

        _connectionRepository = _serviceProvider.GetRequiredService<IAIConnectionRepository>();
        _profileRepository = _serviceProvider.GetRequiredService<IAIProfileRepository>();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public void IAIOperationTracker_ResolvesToTheSingletonAIOperationTracker()
    {
        var tracker = _serviceProvider.GetRequiredService<IAIOperationTracker>();

        tracker.ShouldNotBeNull();
        tracker.ShouldBeOfType<AIOperationTracker>();

        // Singleton lifetime: resolving again must yield the same instance.
        _serviceProvider.GetRequiredService<IAIOperationTracker>().ShouldBeSameAs(tracker);
    }

    [Fact]
    public async Task IAIChatClientFactory_ResolvesAndAppliesTrackingMiddleware_ViaRealDiContainer()
    {
        var profile = await SeedConnectionAndProfileAsync(AICapability.Chat, "chat-profile", "chat-model");

        var factory = _serviceProvider.GetRequiredService<IAIChatClientFactory>();
        var client = await factory.CreateClientAsync(profile);

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task IAIEmbeddingGeneratorFactory_ResolvesAndAppliesTrackingMiddleware_ViaRealDiContainer()
    {
        var profile = await SeedConnectionAndProfileAsync(AICapability.Embedding, "embedding-profile", "embedding-model");

        var factory = _serviceProvider.GetRequiredService<IAIEmbeddingGeneratorFactory>();
        var generator = await factory.CreateGeneratorAsync(profile);

        generator.ShouldNotBeNull();
    }

    [Fact]
    public async Task IAISpeechToTextClientFactory_ResolvesAndAppliesTrackingMiddleware_ViaRealDiContainer()
    {
        var profile = await SeedConnectionAndProfileAsync(AICapability.SpeechToText, "stt-profile", "stt-model");

        var factory = _serviceProvider.GetRequiredService<IAISpeechToTextClientFactory>();
        var client = await factory.CreateClientAsync(profile);

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task IAIImageGeneratorFactory_ResolvesAndAppliesTrackingMiddleware_ViaRealDiContainer()
    {
        var profile = await SeedConnectionAndProfileAsync(AICapability.ImageGeneration, "image-profile", "image-model");

        var factory = _serviceProvider.GetRequiredService<Umbraco.AI.Core.ImageGeneration.IAIImageGeneratorFactory>();
        var generator = await factory.CreateGeneratorAsync(profile);

        generator.ShouldNotBeNull();
    }

    /// <summary>
    /// Seeds a fresh connection + profile pair (one per test) targeting the fake provider, so each
    /// factory can resolve a configured capability and produce a real (wrapped) client/generator.
    /// </summary>
    private async Task<AIProfile> SeedConnectionAndProfileAsync(AICapability capability, string alias, string modelId)
    {
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithName($"Tracker DI Connection ({alias})")
            .WithProviderId("fake-provider")
            .WithSettings(new FakeProviderSettings { ApiKey = "test-api-key" })
            .IsActive(true)
            .Build();

        await _connectionRepository.SaveAsync(connection);

        var profile = new AIProfileBuilder()
            .WithAlias(alias)
            .WithName(alias)
            .WithCapability(capability)
            .WithConnectionId(connectionId)
            .WithModel("fake-provider", modelId)
            .Build();

        await _profileRepository.SaveAsync(profile);

        return profile;
    }

    /// <summary>
    /// Registers AI services directly, simulating what AddUmbracoAICore does but bypassing
    /// Umbraco's collection builder pattern (which requires TypeLoader that can't be mocked) —
    /// following the same pattern established by <c>ServiceResolutionTests</c> and
    /// <c>EndToEndServiceFlowTests</c>. The tracking-middleware collections below are the one
    /// deliberate departure: instead of an empty enumerable, each capability's collection is backed
    /// by its real <c>AITracking*Middleware</c> resolved from the container, registered as its own
    /// concrete type — exactly how <c>CollectionBuilderBase.RegisterTypes</c>/<c>CreateItem</c> would
    /// register and resolve an appended middleware type in production.
    /// </summary>
    private static void RegisterAIServices(
        IServiceCollection services,
        IConfiguration configuration,
        FakeAIProvider fakeProvider)
    {
        // Register configuration (required by AIEditableModelResolver)
        services.AddSingleton<IConfiguration>(configuration);

        // Bind AIOptions
        services.Configure<AIOptions>(configuration.GetSection("Umbraco:AI"));

        // Experimental feature gate
        services.Configure<Umbraco.AI.Core.Settings.AIExperimentalOptions>(configuration.GetSection("Umbraco:AI:Experimental"));
        services.AddSingleton<Umbraco.AI.Core.Settings.IAIExperimentalFeatures, Umbraco.AI.Core.Settings.AIExperimentalFeatures>();

        // Provider infrastructure
        services.AddSingleton<IAICapabilityFactory, AICapabilityFactory>();
        services.AddSingleton<IAIEditableModelSchemaBuilder, AIEditableModelSchemaBuilder>();
        services.AddSingleton<IAIProviderInfrastructure, AIProviderInfrastructure>();

        // Register the fake provider (in real scenario, these are auto-discovered)
        services.AddSingleton<IAIProvider>(fakeProvider);

        // Create provider collection from registered providers
        services.AddSingleton<AIProviderCollection>(sp =>
        {
            var providers = sp.GetServices<IAIProvider>();
            return new AIProviderCollection(() => providers);
        });

        // Tracking middleware collections — backed by the REAL AITracking*Middleware types,
        // constructed via the DI container (see remarks on the class above / RegisterAIServices).
        services.AddSingleton<AITrackingChatMiddleware>();
        services.AddSingleton<AIChatMiddlewareCollection>(sp =>
            new AIChatMiddlewareCollection(() => new IAIChatMiddleware[] { sp.GetRequiredService<AITrackingChatMiddleware>() }));

        services.AddSingleton<AITrackingEmbeddingMiddleware>();
        services.AddSingleton<AIEmbeddingMiddlewareCollection>(sp =>
            new AIEmbeddingMiddlewareCollection(() => new IAIEmbeddingMiddleware[] { sp.GetRequiredService<AITrackingEmbeddingMiddleware>() }));

        services.AddSingleton<AITrackingSpeechToTextMiddleware>();
        services.AddSingleton<AISpeechToTextMiddlewareCollection>(sp =>
            new AISpeechToTextMiddlewareCollection(() => new IAISpeechToTextMiddleware[] { sp.GetRequiredService<AITrackingSpeechToTextMiddleware>() }));

        services.AddSingleton<Umbraco.AI.Core.ImageGeneration.AITrackingImageGenerationMiddleware>();
        services.AddSingleton<Umbraco.AI.Core.ImageGeneration.AIImageGenerationMiddlewareCollection>(sp =>
            new Umbraco.AI.Core.ImageGeneration.AIImageGenerationMiddlewareCollection(() =>
                new Umbraco.AI.Core.ImageGeneration.IAIImageGenerationMiddleware[]
                {
                    sp.GetRequiredService<Umbraco.AI.Core.ImageGeneration.AITrackingImageGenerationMiddleware>(),
                }));

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

        // Guardrail system
        services.AddSingleton<IAIGuardrailRepository, InMemoryAIGuardrailRepository>();
        services.AddSingleton<IAIGuardrailService, AIGuardrailService>();

        // Context system
        services.AddSingleton<IAIContextRepository, InMemoryAIContextRepository>();
        services.AddSingleton<IAIContextService, AIContextService>();

        // Client factories — the four factories under test
        services.AddSingleton<IAIChatClientFactory, AIChatClientFactory>();
        services.AddSingleton<IAIEmbeddingGeneratorFactory, AIEmbeddingGeneratorFactory>();
        services.AddSingleton<IAISpeechToTextClientFactory, AISpeechToTextClientFactory>();
        services.AddSingleton<Umbraco.AI.Core.ImageGeneration.IAIImageGeneratorFactory, Umbraco.AI.Core.ImageGeneration.AIImageGeneratorFactory>();

        // Tool system (empty collection / no scopes — not exercised by this DI smoke test)
        services.AddSingleton(new AIToolScopeCollection(() => []));
        services.AddSingleton(new AIToolCollection(() => []));
        services.AddSingleton<IAIFunctionFactory, Umbraco.AI.Core.Tools.AIFunctionFactory>();

        // Observability: the real IAIOperationTracker singleton, backed by Moq'd audit/usage
        // services. Only the tracker's own DI-constructibility and the tracking middleware's
        // ability to wrap a client are under test here — TrackAsync/BeginAsync are never invoked
        // (see AIOperationTrackerTests for behavioral coverage of the tracker itself).
        services.Configure<AIAuditLogOptions>(configuration.GetSection("Umbraco:AI:AuditLog"));
        services.Configure<AIAnalyticsOptions>(configuration.GetSection("Umbraco:AI:Analytics"));
        services.AddSingleton(new Mock<IAIAuditLogService>().Object);
        services.AddSingleton(new Mock<IAIAuditLogFactory>().Object);
        services.AddSingleton(new Mock<IAIUsageRecordingService>().Object);
        services.AddSingleton(new Mock<IAIUsageRecordFactory>().Object);
        services.AddSingleton<IAIOperationTracker, AIOperationTracker>();

        // Required for options
        services.AddLogging();
        services.AddOptions();
    }
}

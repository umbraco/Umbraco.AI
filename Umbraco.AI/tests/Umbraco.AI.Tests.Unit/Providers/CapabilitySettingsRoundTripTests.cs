using Microsoft.Extensions.AI;
using Moq;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.ImageGeneration;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.SpeechToText;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;

#pragma warning disable MEAI001 // ISpeechToTextClient / IImageGenerator are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Exercises the experimental image-generation capability surface

namespace Umbraco.AI.Tests.Unit.Providers;

/// <summary>
/// Proves that provider-declared capability settings survive the whole trip for every capability: stored on
/// a profile, resolved by the factory, handed to the capability, and applied to the request's options.
/// </summary>
/// <remarks>
/// <para>
/// These are the guards that make the two-parameter capability bases a usable extension point rather than a
/// trap. The schema, API and editor layers are capability-agnostic already, so a base published without the
/// factory threading or the request-time decorator would give a third-party provider a rendered editor whose
/// values are silently dropped — a failure that looks exactly like success.
/// </para>
/// <para>
/// Deliberately end-to-end through the real factories, real configured-capability wrappers and real bases.
/// Mocking any of those three would leave the test passing while the thing it exists to catch stays broken.
/// </para>
/// </remarks>
public class CapabilitySettingsRoundTripTests
{
    private const string ProviderId = "fake-provider";
    private const string ModelId = "fake-model-1";
    private const string StoredValue = "from-profile";

    private readonly Mock<IAIConnectionService> _connectionService = new();
    private readonly Mock<IAIEditableModelResolver> _modelResolver = new();
    private readonly Mock<IAIRuntimeContextAccessor> _contextAccessor = new();
    private readonly Mock<IAIRuntimeContextScopeProvider> _scopeProvider = new();

    private readonly AIRuntimeContextContributorCollection _contributors =
        new(Enumerable.Empty<IAIRuntimeContextContributor>);

    public CapabilitySettingsRoundTripTests()
    {
        // The outermost scoped wrapper opens a runtime-context scope per call, so give it a real context to
        // populate. None of this is under test; without it the wrapper faults before the request runs.
        var context = new AIRuntimeContext([]);
        var scope = new Mock<IAIRuntimeContextScope>();
        scope.Setup(x => x.Context).Returns(context);

        _contextAccessor.Setup(x => x.Context).Returns((AIRuntimeContext?)null);
        _scopeProvider
            .Setup(x => x.CreateScope(It.IsAny<IEnumerable<AIRequestContextItem>>()))
            .Returns(() =>
            {
                _contextAccessor.Setup(x => x.Context).Returns(context);
                return scope.Object;
            });
        _scopeProvider
            .Setup(x => x.CreateScope())
            .Returns(() =>
            {
                _contextAccessor.Setup(x => x.Context).Returns(context);
                return scope.Object;
            });
    }

    [Fact]
    public async Task Embedding_CapabilitySettings_ReachTheRequestOptions()
    {
        // Arrange
        var recorder = new FakeEmbeddingGenerator();
        var provider = new FakeAIProvider(ProviderId, "Fake Provider");
        var capability = new TestEmbeddingCapability(provider, recorder);
        provider.WithCapability<IAIEmbeddingCapability>(capability);

        var profile = ArrangeProfile<IAIConfiguredEmbeddingCapability>(
            provider,
            AICapability.Embedding,
            new AIConfiguredEmbeddingCapability(capability, ConnectionSettings));

        var factory = new AIEmbeddingGeneratorFactory(
            _connectionService.Object,
            new AIEmbeddingMiddlewareCollection(Enumerable.Empty<IAIEmbeddingMiddleware>),
            _contextAccessor.Object,
            _scopeProvider.Object,
            _contributors,
            _modelResolver.Object);

        // Act
        var generator = await factory.CreateGeneratorAsync(profile);
        await generator.GenerateAsync(["hello"]);

        // Assert
        AssertApplied(recorder.ReceivedOptions.ShouldHaveSingleItem()?.AdditionalProperties);
    }

    [Fact]
    public async Task SpeechToText_CapabilitySettings_ReachTheRequestOptions()
    {
        // Arrange
        var recorder = new FakeSpeechToTextClient();
        var provider = new FakeAIProvider(ProviderId, "Fake Provider");
        var capability = new TestSpeechToTextCapability(provider, recorder);
        provider.WithCapability<IAISpeechToTextCapability>(capability);

        var profile = ArrangeProfile<IAIConfiguredSpeechToTextCapability>(
            provider,
            AICapability.SpeechToText,
            new AIConfiguredSpeechToTextCapability(capability, ConnectionSettings));

        var factory = new AISpeechToTextClientFactory(
            _connectionService.Object,
            new AISpeechToTextMiddlewareCollection(Enumerable.Empty<IAISpeechToTextMiddleware>),
            _contextAccessor.Object,
            _scopeProvider.Object,
            _contributors,
            _modelResolver.Object);

        // Act
        var client = await factory.CreateClientAsync(profile);
        await client.GetTextAsync(new MemoryStream([1, 2, 3]));

        // Assert
        AssertApplied(recorder.ReceivedOptions.ShouldHaveSingleItem()?.AdditionalProperties);
    }

    [Fact]
    public async Task ImageGeneration_CapabilitySettings_ReachTheRequestOptions()
    {
        // Arrange
        var recorder = new FakeImageGenerator();
        var provider = new FakeAIProvider(ProviderId, "Fake Provider");
        var capability = new TestImageGeneratorCapability(provider, recorder);
        provider.WithCapability<IAIImageGeneratorCapability>(capability);

        var profile = ArrangeProfile<IAIConfiguredImageGeneratorCapability>(
            provider,
            AICapability.ImageGeneration,
            new AIConfiguredImageGeneratorCapability(capability, ConnectionSettings));

        var factory = new AIImageGeneratorFactory(
            _connectionService.Object,
            new AIImageGenerationMiddlewareCollection(Enumerable.Empty<IAIImageGenerationMiddleware>),
            _contextAccessor.Object,
            _scopeProvider.Object,
            _contributors,
            _modelResolver.Object);

        // Act
        var generator = await factory.CreateGeneratorAsync(profile);
        await generator.GenerateAsync(new ImageGenerationRequest { Prompt = "a cat" });

        // Assert
        AssertApplied(recorder.ReceivedOptions.ShouldHaveSingleItem()?.AdditionalProperties);
    }

    [Fact]
    public async Task ProfileWithoutCapabilitySettings_LeavesTheRequestUntouched()
    {
        // Arrange — the shape every profile has before a provider declares any settings, and the shape an
        // upgraded installation resolves on its first request
        var recorder = new FakeEmbeddingGenerator();
        var provider = new FakeAIProvider(ProviderId, "Fake Provider");
        var capability = new TestEmbeddingCapability(provider, recorder);
        provider.WithCapability<IAIEmbeddingCapability>(capability);

        var profile = ArrangeProfile<IAIConfiguredEmbeddingCapability>(
            provider,
            AICapability.Embedding,
            new AIConfiguredEmbeddingCapability(capability, ConnectionSettings),
            storeCapabilitySettings: false);

        var factory = new AIEmbeddingGeneratorFactory(
            _connectionService.Object,
            new AIEmbeddingMiddlewareCollection(Enumerable.Empty<IAIEmbeddingMiddleware>),
            _contextAccessor.Object,
            _scopeProvider.Object,
            _contributors,
            _modelResolver.Object);

        // Act
        var generator = await factory.CreateGeneratorAsync(profile);
        await generator.GenerateAsync(["hello"]);

        // Assert — no decorator, so nothing is invented on the caller's behalf
        recorder.ReceivedOptions.ShouldHaveSingleItem().ShouldBeNull();
    }

    private static FakeProviderSettings ConnectionSettings => new() { ApiKey = "test-key" };

    private static void AssertApplied(AdditionalPropertiesDictionary? additionalProperties)
    {
        additionalProperties.ShouldNotBeNull();
        additionalProperties["applied"].ShouldBe(StoredValue);
        // The model reaches the apply hook too, so a provider can gate a setting the model rejects.
        additionalProperties["model"].ShouldBe(ModelId);
    }

    /// <summary>
    /// Wires the connection service and resolver so the real factory reaches the given configured capability,
    /// with a stored bag standing in for the profile's persisted column.
    /// </summary>
    private AIProfile ArrangeProfile<TConfigured>(
        IAIProvider provider,
        AICapability capability,
        TConfigured configured,
        bool storeCapabilitySettings = true)
        where TConfigured : class, IAIConfiguredCapability
    {
        var connectionId = Guid.NewGuid();
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(ProviderId)
            .WithSettings(ConnectionSettings)
            .IsActive(true)
            .Build();

        var profile = new AIProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel(ProviderId, ModelId)
            .WithCapability(capability)
            .Build();

        var configuredProvider = new Mock<IAIConfiguredProvider>();
        configuredProvider.Setup(x => x.Provider).Returns(provider);
        configuredProvider.Setup(x => x.GetCapability<TConfigured>()).Returns(configured);
        configuredProvider.Setup(x => x.GetCapabilities()).Returns(new IAIConfiguredCapability[] { configured });

        _connectionService
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);
        _connectionService
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProvider.Object);

        // Stands in for the editable-model pipeline, which has its own tests. What matters here is that
        // whatever it returns is what the capability receives.
        _modelResolver
            .Setup(x => x.ResolveModel(It.IsAny<Type>(), It.IsAny<object?>(), It.IsAny<AIEditableModelSchema?>()))
            .Returns(new TestCapabilitySettings { Applied = StoredValue });

        // A null column short-circuits before the resolver is consulted, which is the pre-declaration shape.
        if (storeCapabilitySettings)
        {
            profile.CapabilitySettings = new Dictionary<string, object?> { ["applied"] = StoredValue };
        }

        return profile;
    }

    /// <summary>The provider-declared settings a third-party package would define.</summary>
    private sealed class TestCapabilitySettings
    {
        public string? Applied { get; set; }
    }

    private sealed class TestEmbeddingCapability(
        IAIProvider provider,
        IEmbeddingGenerator<string, Embedding<float>> inner)
        : AIEmbeddingCapabilityBase<FakeProviderSettings, TestCapabilitySettings>(provider)
    {
        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Models);

        protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(
            FakeProviderSettings settings,
            string? modelId)
            => inner;

        protected override void ApplyCapabilitySettings(
            TestCapabilitySettings capabilitySettings,
            string? modelId,
            EmbeddingGenerationOptions options)
            => Record(options.AdditionalProperties ??= new AdditionalPropertiesDictionary(), capabilitySettings, modelId);
    }

    private sealed class TestSpeechToTextCapability(IAIProvider provider, ISpeechToTextClient inner)
        : AISpeechToTextCapabilityBase<FakeProviderSettings, TestCapabilitySettings>(provider)
    {
        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Models);

        protected override ISpeechToTextClient CreateClient(FakeProviderSettings settings, string? modelId)
            => inner;

        protected override void ApplyCapabilitySettings(
            TestCapabilitySettings capabilitySettings,
            string? modelId,
            SpeechToTextOptions options)
            => Record(options.AdditionalProperties ??= new AdditionalPropertiesDictionary(), capabilitySettings, modelId);
    }

    private sealed class TestImageGeneratorCapability(IAIProvider provider, IImageGenerator inner)
        : AIImageGeneratorCapabilityBase<FakeProviderSettings, TestCapabilitySettings>(provider)
    {
        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Models);

        protected override IImageGenerator CreateGenerator(FakeProviderSettings settings, string? modelId)
            => inner;

        protected override void ApplyCapabilitySettings(
            TestCapabilitySettings capabilitySettings,
            string? modelId,
            ImageGenerationOptions options)
            => Record(options.AdditionalProperties ??= new AdditionalPropertiesDictionary(), capabilitySettings, modelId);
    }

    private static IReadOnlyList<AIModelDescriptor> Models =>
        [new AIModelDescriptor(new AIModelRef(ProviderId, ModelId), "Fake Model 1")];

    /// <summary>
    /// What a real provider's apply hook would do to the request, reduced to something observable.
    /// </summary>
    private static void Record(
        AdditionalPropertiesDictionary additionalProperties,
        TestCapabilitySettings capabilitySettings,
        string? modelId)
    {
        additionalProperties["applied"] = capabilitySettings.Applied;
        additionalProperties["model"] = modelId;
    }
}

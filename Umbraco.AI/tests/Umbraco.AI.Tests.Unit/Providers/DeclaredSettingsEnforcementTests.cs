using System.Drawing;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Tests.Common.Fakes;

#pragma warning disable MEAI001 // ISpeechToTextClient / IImageGenerator are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Exercises the experimental image-generation capability

namespace Umbraco.AI.Tests.Unit.Providers;

/// <summary>
/// A capability's per-model declaration is enforced on every request, not merely shown to the editor.
/// </summary>
/// <remarks>
/// <para>
/// This behaviour used to live in three near-identical provider decorators (OpenAI, Anthropic, Amazon),
/// each of which had to be kept in step with its own declaration by hand, and any new provider that forgot
/// to install one failed silently. It now belongs to the capability bases, driven by the same
/// <see cref="IAICapability.GetSettingsSupport"/> the profile editor reads — so the two cannot disagree.
/// </para>
/// <para>
/// These tests own the filter's behaviour. That the filtered value genuinely leaves the request is proved
/// end-to-end against a serialized request body in the Anthropic provider's wire tests, because a recording
/// test here would stay green if an SDK adapter stopped reading the options at all.
/// </para>
/// </remarks>
public class DeclaredSettingsEnforcementTests
{
    private static readonly FakeProviderSettings Settings = new();

    [Fact]
    public async Task DeclaredUnsupported_SamplingOptions_AreRemoved()
    {
        var recorder = new FakeChatClient();
        var capability = new DeclaringChatCapability(recorder, "restricted-model");

        var client = await CreateClientAsync(capability, "restricted-model");
        await client.GetResponseAsync("hi", new ChatOptions
        {
            Temperature = 0.5f,
            TopP = 0.9f,
            TopK = 40,
            FrequencyPenalty = 0.1f,
            PresencePenalty = 0.2f,
        });

        var options = recorder.ReceivedOptions.ShouldHaveSingleItem();
        options!.Temperature.ShouldBeNull();
        options.TopP.ShouldBeNull();
        options.TopK.ShouldBeNull();
        options.FrequencyPenalty.ShouldBeNull();
        options.PresencePenalty.ShouldBeNull();
    }

    [Fact]
    public async Task DeclaredUnsupported_MaxOutputTokens_IsLeftAlone()
    {
        // Deliberately not strippable: some providers require a token limit, so removing it would fail the
        // request rather than degrade it. A declaration naming it has no core option to act on.
        var recorder = new FakeChatClient();
        var capability = new DeclaringChatCapability(recorder, "restricted-model");

        var client = await CreateClientAsync(capability, "restricted-model");
        await client.GetResponseAsync("hi", new ChatOptions { Temperature = 0.5f, MaxOutputTokens = 128 });

        var options = recorder.ReceivedOptions.ShouldHaveSingleItem();
        options!.Temperature.ShouldBeNull();
        options.MaxOutputTokens.ShouldBe(128);
    }

    [Fact]
    public async Task ModelWithNoDeclaration_OptionsAreUntouched()
    {
        var recorder = new FakeChatClient();
        var capability = new DeclaringChatCapability(recorder, "restricted-model");

        var client = await CreateClientAsync(capability, "permissive-model");
        var options = new ChatOptions { Temperature = 0.5f, TopP = 0.9f };
        await client.GetResponseAsync("hi", options);

        // The very same instance, not a filtered copy: nothing needed changing.
        recorder.ReceivedOptions.ShouldHaveSingleItem().ShouldBeSameAs(options);
    }

    [Fact]
    public async Task CallerModelIdWins_OverTheBoundModel()
    {
        // A caller targeting a different model than the client was created for must be judged on the model
        // the request will actually run against.
        var recorder = new FakeChatClient();
        var capability = new DeclaringChatCapability(recorder, "restricted-model");

        var client = await CreateClientAsync(capability, "permissive-model");
        await client.GetResponseAsync("hi", new ChatOptions { ModelId = "restricted-model", Temperature = 0.5f });

        recorder.ReceivedOptions.ShouldHaveSingleItem()!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task CallerWithoutAModelId_FallsBackToTheBoundModel()
    {
        // Load-bearing rather than defensive: the agent runtime builds its ChatOptions without a ModelId,
        // so the creation-time model is the only signal on that path.
        var recorder = new FakeChatClient();
        var capability = new DeclaringChatCapability(recorder, "restricted-model");

        var client = await CreateClientAsync(capability, "restricted-model");
        await client.GetResponseAsync("hi", new ChatOptions { Temperature = 0.5f });

        recorder.ReceivedOptions.ShouldHaveSingleItem()!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task NoOptions_IsPassedThrough()
    {
        var recorder = new FakeChatClient();
        var capability = new DeclaringChatCapability(recorder, "restricted-model");

        var client = await CreateClientAsync(capability, "restricted-model");
        await client.GetResponseAsync("hi");

        recorder.ReceivedOptions.ShouldHaveSingleItem().ShouldBeNull();
    }

    [Fact]
    public async Task Embedding_DeclaredUnsupportedDimensions_AreRemoved()
    {
        var recorder = new FakeEmbeddingGenerator();
        var capability = new DeclaringEmbeddingCapability(recorder, "restricted-model");

        var generator = await ((IAIEmbeddingCapability)capability)
            .CreateGeneratorAsync(Settings, "restricted-model", CancellationToken.None);
        await generator.GenerateAsync(["hello"], new EmbeddingGenerationOptions { Dimensions = 256 });

        recorder.ReceivedOptions.ShouldHaveSingleItem()!.Dimensions.ShouldBeNull();
    }

    [Fact]
    public async Task Embedding_ModelThatAcceptsDimensions_KeepsThem()
    {
        var recorder = new FakeEmbeddingGenerator();
        var capability = new DeclaringEmbeddingCapability(recorder, "restricted-model");

        var generator = await ((IAIEmbeddingCapability)capability)
            .CreateGeneratorAsync(Settings, "permissive-model", CancellationToken.None);
        await generator.GenerateAsync(["hello"], new EmbeddingGenerationOptions { Dimensions = 256 });

        recorder.ReceivedOptions.ShouldHaveSingleItem()!.Dimensions.ShouldBe(256);
    }

    [Fact]
    public async Task SpeechToText_DeclaredUnsupportedLanguage_IsRemoved()
    {
        var recorder = new FakeSpeechToTextClient();
        var capability = new DeclaringSpeechToTextCapability(recorder, "restricted-model");

        var client = await ((IAISpeechToTextCapability)capability)
            .CreateClientAsync(Settings, "restricted-model", CancellationToken.None);
        await client.GetTextAsync(new MemoryStream([1, 2, 3]), new SpeechToTextOptions { SpeechLanguage = "en" });

        recorder.ReceivedOptions.ShouldHaveSingleItem()!.SpeechLanguage.ShouldBeNull();
    }

    [Fact]
    public async Task SpeechToText_ModelThatAcceptsALanguage_KeepsIt()
    {
        var recorder = new FakeSpeechToTextClient();
        var capability = new DeclaringSpeechToTextCapability(recorder, "restricted-model");

        var client = await ((IAISpeechToTextCapability)capability)
            .CreateClientAsync(Settings, "permissive-model", CancellationToken.None);
        await client.GetTextAsync(new MemoryStream([1, 2, 3]), new SpeechToTextOptions { SpeechLanguage = "en" });

        recorder.ReceivedOptions.ShouldHaveSingleItem()!.SpeechLanguage.ShouldBe("en");
    }

    [Fact]
    public async Task ImageGeneration_DeclaredUnsupportedMediaType_IsRemoved()
    {
        // output_format is a gpt-image parameter with no DALL·E equivalent, so this is a real declaration
        // rather than a hypothetical one.
        var recorder = new FakeImageGenerator();
        var capability = new DeclaringImageCapability(recorder, "restricted-model");

        var generator = await ((IAIImageGeneratorCapability)capability)
            .CreateGeneratorAsync(Settings, "restricted-model", CancellationToken.None);
        await generator.GenerateAsync(
            new ImageGenerationRequest { Prompt = "a cat" },
            new ImageGenerationOptions { MediaType = "image/png", ImageSize = new Size(1024, 1024) });

        var options = recorder.ReceivedOptions.ShouldHaveSingleItem();
        options!.MediaType.ShouldBeNull();
        // Only what was declared is stripped: size was not, so it survives.
        options.ImageSize.ShouldNotBeNull();
    }

    [Fact]
    public async Task ImageGeneration_ModelThatAcceptsMediaType_KeepsIt()
    {
        var recorder = new FakeImageGenerator();
        var capability = new DeclaringImageCapability(recorder, "restricted-model");

        var generator = await ((IAIImageGeneratorCapability)capability)
            .CreateGeneratorAsync(Settings, "permissive-model", CancellationToken.None);
        await generator.GenerateAsync(
            new ImageGenerationRequest { Prompt = "a cat" },
            new ImageGenerationOptions { MediaType = "image/png" });

        recorder.ReceivedOptions.ShouldHaveSingleItem()!.MediaType.ShouldBe("image/png");
    }

    private static Task<IChatClient> CreateClientAsync(IAIChatCapability capability, string modelId)
        => capability.CreateClientAsync(Settings, modelId, CancellationToken.None);

    /// <summary>
    /// A capability that declares the sampling group unsupported for one named model, standing in for the
    /// per-model predicate a real provider keeps.
    /// </summary>
    private sealed class DeclaringChatCapability(IChatClient inner, string restrictedModelId)
        : AIChatCapabilityBase<FakeProviderSettings>(new FakeAIProvider())
    {
        public override AIModelSettingsSupport GetSettingsSupport(string modelId)
            => modelId == restrictedModelId
                ? new AIModelSettingsSupport
                {
                    // Includes a key core cannot act on, to prove the unknown one is ignored rather than
                    // throwing or blocking the rest.
                    UnsupportedProfileSettings = [.. AIProfileSettingKeys.Sampling, "maxTokens"],
                }
                : AIModelSettingsSupport.Default;

        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>([]);

        protected override IChatClient CreateClient(FakeProviderSettings settings, string? modelId) => inner;
    }

    private sealed class DeclaringEmbeddingCapability(
        IEmbeddingGenerator<string, Embedding<float>> inner,
        string restrictedModelId)
        : AIEmbeddingCapabilityBase<FakeProviderSettings>(new FakeAIProvider())
    {
        public override AIModelSettingsSupport GetSettingsSupport(string modelId)
            => modelId == restrictedModelId
                ? new AIModelSettingsSupport { UnsupportedProfileSettings = [AIProfileSettingKeys.Dimensions] }
                : AIModelSettingsSupport.Default;

        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>([]);

        protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(
            FakeProviderSettings settings,
            string? modelId)
            => inner;
    }

    private sealed class DeclaringSpeechToTextCapability(ISpeechToTextClient inner, string restrictedModelId)
        : AISpeechToTextCapabilityBase<FakeProviderSettings>(new FakeAIProvider())
    {
        public override AIModelSettingsSupport GetSettingsSupport(string modelId)
            => modelId == restrictedModelId
                ? new AIModelSettingsSupport { UnsupportedProfileSettings = [AIProfileSettingKeys.Language] }
                : AIModelSettingsSupport.Default;

        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>([]);

        protected override ISpeechToTextClient CreateClient(FakeProviderSettings settings, string? modelId) => inner;
    }

    private sealed class DeclaringImageCapability(IImageGenerator inner, string restrictedModelId)
        : AIImageGeneratorCapabilityBase<FakeProviderSettings>(new FakeAIProvider())
    {
        public override AIModelSettingsSupport GetSettingsSupport(string modelId)
            => modelId == restrictedModelId
                ? new AIModelSettingsSupport { UnsupportedProfileSettings = [AIProfileSettingKeys.MediaType] }
                : AIModelSettingsSupport.Default;

        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>([]);

        protected override IImageGenerator CreateGenerator(FakeProviderSettings settings, string? modelId) => inner;
    }
}

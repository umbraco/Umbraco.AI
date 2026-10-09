using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using OpenAI.Responses;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.OpenAI.Tests.Unit.Fakes;

namespace Umbraco.AI.OpenAI.Tests.Unit;

/// <summary>
/// Pins down how the reasoning effort reaches the OpenAI wire, asserted against a captured request body
/// rather than a live call.
/// </summary>
/// <remarks>
/// The capability sets it through <see cref="ChatOptions.RawRepresentationFactory"/> on an otherwise empty
/// <see cref="CreateResponseOptions"/>, which only works because the Microsoft.Extensions.AI adapter fills
/// the rest of the request (model, input, token limits) around it. If that changes, this fails rather than
/// the setting silently going missing — or worse, a request going out without a model.
/// </remarks>
public class OpenAIReasoningEffortWireTests
{
    [Theory]
    [InlineData("none", "none")]
    [InlineData("minimal", "low")]
    [InlineData("low", "low")]
    [InlineData("medium", "medium")]
    [InlineData("high", "high")]
    [InlineData("xhigh", "xhigh")]
    [InlineData("max", "max")]
    [InlineData("  MAX  ", "max")]
    public async Task CapabilitySettings_Luna_SendsSupportedEffort(string configured, string expected)
    {
        var handler = new CapturingHttpMessageHandler();
        using var client = await CreateCapabilityClientAsync(handler, "gpt-6-luna", configured);

        await SendAndIgnoreFailureAsync(client, new ChatOptions { MaxOutputTokens = 64 });

        using var body = JsonDocument.Parse(handler.RequestBodies.ShouldHaveSingleItem());
        body.RootElement.GetProperty("reasoning").GetProperty("effort").GetString().ShouldBe(expected);
        body.RootElement.GetProperty("model").GetString().ShouldBe("gpt-6-luna");
        body.RootElement.GetProperty("max_output_tokens").GetInt32().ShouldBe(64);
        body.RootElement.GetProperty("input").GetRawText().ShouldContain("hello");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ultra")]
    public async Task CapabilitySettings_Luna_UnsetOrUnknownEffort_IsOmitted(string? effort)
    {
        var handler = new CapturingHttpMessageHandler();
        using var client = await CreateCapabilityClientAsync(handler, "gpt-6-luna", effort);

        await SendAndIgnoreFailureAsync(client, new ChatOptions());

        using var body = JsonDocument.Parse(handler.RequestBodies.ShouldHaveSingleItem());
        body.RootElement.TryGetProperty("reasoning", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("gpt-5.6-luna", "minimal", "minimal")]
    [InlineData("gpt-5.6", "none", "none")]
    [InlineData("gpt-5.6", "low", "low")]
    [InlineData("gpt-5.6", "medium", "medium")]
    [InlineData("gpt-5.6", "high", "high")]
    [InlineData("o3-mini", "low", "low")]
    [InlineData("gpt-6-luna-2026-09-01", "max", "max")]
    [InlineData("GPT-6-LUNA", "minimal", "low")]
    public async Task CapabilitySettings_ExistingModelsAndLunaSnapshots_PreserveEffort(
        string modelId, string configured, string expected)
    {
        var handler = new CapturingHttpMessageHandler();
        using var client = await CreateCapabilityClientAsync(handler, modelId, configured);

        await SendAndIgnoreFailureAsync(client, new ChatOptions());

        using var body = JsonDocument.Parse(handler.RequestBodies.ShouldHaveSingleItem());
        body.RootElement.GetProperty("reasoning").GetProperty("effort").GetString().ShouldBe(expected);
    }

    [Theory]
    [InlineData("gpt-4o", "minimal")]
    [InlineData("gpt-5-chat-latest", "low")]
    [InlineData("gpt-5.6-chat", "low")]
    [InlineData("gpt-5.6-chat", "max")]
    [InlineData("gpt-5.6-chat-latest", "xhigh")]
    [InlineData("gpt-4o", "max")]
    [InlineData("gpt-5.6", "ultra")]
    [InlineData("gpt-6-sol", "minimal")]
    [InlineData("gpt-6-luna-chat-latest", "max")]
    [InlineData("future-model", "low")]
    public async Task CapabilitySettings_UnsupportedEffortOrModel_IsOmitted(string modelId, string effort)
    {
        var handler = new CapturingHttpMessageHandler();
        using var client = await CreateCapabilityClientAsync(handler, modelId, effort);

        await SendAndIgnoreFailureAsync(client, new ChatOptions());

        using var body = JsonDocument.Parse(handler.RequestBodies.ShouldHaveSingleItem());
        body.RootElement.TryGetProperty("reasoning", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("gpt-5.6", "xhigh")]
    [InlineData("gpt-5.6", "max")]
    [InlineData("gpt-5.6-sol", "xhigh")]
    [InlineData("gpt-5.6-sol", "max")]
    [InlineData("gpt-5.6-terra", "xhigh")]
    [InlineData("gpt-5.6-terra", "max")]
    [InlineData("gpt-5.6-luna", "xhigh")]
    [InlineData("gpt-5.6-luna", "max")]
    [InlineData("gpt-5.6-2026-09-01", "max")]
    [InlineData("gpt-5.6-sol-2026-09-01", "max")]
    [InlineData("GPT-5.6-LUNA", "xhigh")]
    public async Task CapabilitySettings_Gpt56_SendsExtendedEffort(string modelId, string effort)
    {
        var handler = new CapturingHttpMessageHandler();
        using var client = await CreateCapabilityClientAsync(handler, modelId, effort);

        await SendAndIgnoreFailureAsync(client, new ChatOptions());

        using var body = JsonDocument.Parse(handler.RequestBodies.ShouldHaveSingleItem());
        body.RootElement.GetProperty("model").GetString().ShouldBe(modelId);
        body.RootElement.GetProperty("reasoning").GetProperty("effort").GetString().ShouldBe(effort);
    }

    [Theory]
    [InlineData("gpt-5", "xhigh")]
    [InlineData("gpt-5", "max")]
    [InlineData("gpt-5.5", "xhigh")]
    [InlineData("gpt-5.5", "max")]
    [InlineData("gpt-5.4", "max")]
    [InlineData("o1", "xhigh")]
    [InlineData("o3-mini", "xhigh")]
    [InlineData("o3-mini", "max")]
    [InlineData("o4-mini", "max")]
    [InlineData("gpt-5.60", "max")]
    [InlineData("gpt-5.6x", "xhigh")]
    [InlineData("gpt-5.5", "  MAX  ")]
    public async Task CapabilitySettings_OtherReasoningModels_ExtendedEffortFallsBackToHigh(
        string modelId, string effort)
    {
        var handler = new CapturingHttpMessageHandler();
        using var client = await CreateCapabilityClientAsync(handler, modelId, effort);

        await SendAndIgnoreFailureAsync(client, new ChatOptions());

        using var body = JsonDocument.Parse(handler.RequestBodies.ShouldHaveSingleItem());
        body.RootElement.GetProperty("reasoning").GetProperty("effort").GetString().ShouldBe("high");
    }

    [Theory]
    [InlineData("gpt-5.6-luna", "gpt-6-luna", "low")]
    [InlineData("gpt-6-luna", "gpt-5.6-luna", "minimal")]
    public async Task CapabilitySettings_RequestModelOverride_UsesItsEffortVocabulary(
        string boundModel, string requestModel, string expected)
    {
        var handler = new CapturingHttpMessageHandler();
        using var client = await CreateCapabilityClientAsync(handler, boundModel, "minimal");

        await SendAndIgnoreFailureAsync(client, new ChatOptions { ModelId = requestModel });

        using var body = JsonDocument.Parse(handler.RequestBodies.ShouldHaveSingleItem());
        body.RootElement.GetProperty("model").GetString().ShouldBe(requestModel);
        body.RootElement.GetProperty("reasoning").GetProperty("effort").GetString().ShouldBe(expected);
    }

    [Theory]
    [InlineData("gpt-5.6", "o3-mini", "high")]
    [InlineData("o3-mini", "gpt-5.6", "max")]
    [InlineData("gpt-6-luna", "gpt-5.5", "high")]
    [InlineData("gpt-5.5", "gpt-6-luna", "max")]
    public async Task CapabilitySettings_RequestModelOverride_UsesItsExtendedEffortSupport(
        string boundModel, string requestModel, string expected)
    {
        var handler = new CapturingHttpMessageHandler();
        using var client = await CreateCapabilityClientAsync(handler, boundModel, "max");

        await SendAndIgnoreFailureAsync(client, new ChatOptions { ModelId = requestModel });

        using var body = JsonDocument.Parse(handler.RequestBodies.ShouldHaveSingleItem());
        body.RootElement.GetProperty("model").GetString().ShouldBe(requestModel);
        body.RootElement.GetProperty("reasoning").GetProperty("effort").GetString().ShouldBe(expected);
    }

    [Fact]
    public async Task CapabilitySettings_Luna_ChainsFactoryAndPreservesCallerOptions()
    {
        var handler = new CapturingHttpMessageHandler();
        using var client = await CreateCapabilityClientAsync(handler, "gpt-6-luna", "max");
        var raw = new CreateResponseOptions { StoredOutputEnabled = false };
        var factoryCalls = 0;
        var options = new ChatOptions
        {
            Temperature = 0.5f,
            TopP = 0.9f,
            RawRepresentationFactory = _ => { factoryCalls++; return raw; },
        };
        var originalFactory = options.RawRepresentationFactory;

        await SendAndIgnoreFailureAsync(client, options);

        using var body = JsonDocument.Parse(handler.RequestBodies.ShouldHaveSingleItem());
        body.RootElement.GetProperty("reasoning").GetProperty("effort").GetString().ShouldBe("max");
        body.RootElement.GetProperty("store").GetBoolean().ShouldBeFalse();
        body.RootElement.TryGetProperty("temperature", out _).ShouldBeFalse();
        body.RootElement.TryGetProperty("top_p", out _).ShouldBeFalse();
        factoryCalls.ShouldBe(1);
        options.RawRepresentationFactory.ShouldBeSameAs(originalFactory);
        options.Temperature.ShouldBe(0.5f);
        options.TopP.ShouldBe(0.9f);
    }

    [Theory]
    [InlineData("gpt-6-luna", "minimal", "low")]
    [InlineData("gpt-5.6", "xhigh", "xhigh")]
    [InlineData("gpt-5.6", "max", "max")]
    [InlineData("o3-mini", "xhigh", "high")]
    [InlineData("o3-mini", "max", "high")]
    public async Task CapabilitySettings_StreamingSendsModelAppropriateEffort(
        string modelId, string effort, string expected)
    {
        var handler = new CapturingHttpMessageHandler();
        using var client = await CreateCapabilityClientAsync(handler, modelId, effort);
        await Should.ThrowAsync<ClientResultException>(async () =>
        {
            await foreach (var update in client.GetStreamingResponseAsync("hello")) { }
        });

        using var body = JsonDocument.Parse(handler.RequestBodies.ShouldHaveSingleItem());
        body.RootElement.GetProperty("stream").GetBoolean().ShouldBeTrue();
        body.RootElement.GetProperty("reasoning").GetProperty("effort").GetString().ShouldBe(expected);
    }

    [Fact]
    public async Task RawRepresentationFactory_SetsReasoningEffort_ReachesTheRequestBodyWithTheModel()
    {
        // Arrange
        var handler = new CapturingHttpMessageHandler();
        var chatClient = CreateChatClient(handler, "gpt-5.6");

        var options = new ChatOptions
        {
            MaxOutputTokens = 64,
            RawRepresentationFactory = _ =>
            {
                var raw = new CreateResponseOptions();
                raw.ReasoningOptions ??= new ResponseReasoningOptions();
                raw.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.Low;
                return raw;
            },
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, options);

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"effort\":\"low\"");
        // The adapter must still supply everything the representation left empty.
        body.ShouldContain("\"model\":\"gpt-5.6\"");
        body.ShouldContain("hello");
    }

    private static IChatClient CreateChatClient(CapturingHttpMessageHandler handler, string modelId)
        => new OpenAIClient(
                new ApiKeyCredential("test-key"),
                new OpenAIClientOptions
                {
                    Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
                    RetryPolicy = new ClientRetryPolicy(0),
                })
            .GetResponsesClient()
            .AsIChatClient(modelId);

    private static Task<IChatClient> CreateCapabilityClientAsync(
        CapturingHttpMessageHandler handler, string modelId, string? effort)
        => ((IAIChatCapability)new CapturingChatCapability(handler)).CreateClientAsync(
            new OpenAIProviderSettings { ApiKey = "test-key" },
            new OpenAIChatCapabilitySettings { ReasoningEffort = effort },
            modelId, cancellationToken: default);

    // Substitute only the transport-backed SDK client. Exercise the real capability settings and
    // declaration decorators, including bound/request model resolution and streaming.
    private sealed class CapturingChatCapability(CapturingHttpMessageHandler handler)
        : OpenAIChatCapability(
            new OpenAIProvider(new FakeProviderInfrastructure(), new MemoryCache(new MemoryCacheOptions())),
            logger: null)
    {
        protected override IChatClient CreateClient(OpenAIProviderSettings settings, string? modelId)
            => CreateChatClient(handler, modelId!);
    }

    private static async Task SendAndIgnoreFailureAsync(IChatClient chatClient, ChatOptions options)
    {
        // Require the handler's HTTP failure, so serialization/decorator errors cannot be swallowed.
        await Should.ThrowAsync<ClientResultException>(() => chatClient.GetResponseAsync("hello", options));
    }
}

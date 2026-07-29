using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// End-to-end proof that a per-model declaration reaches the wire: the client is built the way production
/// builds it, through the capability, and the assertion is on the serialized request body.
/// </summary>
/// <remarks>
/// <para>
/// This is the shape of test that would have caught the original <c>temperature</c> 400 (#256). It now also
/// covers the plumbing that replaced the provider's own filter: the capability declares which models reject
/// the sampling parameters, and the core base installs the decorator that strips them. Nothing here
/// constructs that decorator, so a base that stopped wrapping fails this rather than passing.
/// </para>
/// <para>
/// Asserting on the body rather than on the <see cref="ChatOptions"/> handed downstream is deliberate: the
/// filter only matters while the Microsoft.Extensions.AI adapter reads
/// <see cref="ChatOptions.Temperature"/> at all, and a recording test would stay green if it stopped.
/// </para>
/// </remarks>
public class AnthropicSamplingParameterWireTests
{
    [Fact]
    public async Task ModelRejectingSamplingParameters_TheyDoNotReachTheRequest()
    {
        // Arrange — Claude Opus 5 rejects the sampling parameters
        var (chatClient, handler) = await CreateCapabilityClientAsync("claude-opus-5");

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions { Temperature = 0.5f, TopP = 0.9f });

        // Assert
        var body = LastRequestBody(handler);
        body.ShouldNotContain("temperature");
        body.ShouldNotContain("top_p");
        body.ShouldContain("hello");
    }

    [Fact]
    public async Task ModelAcceptingSamplingParameters_TheyReachTheRequest()
    {
        // Arrange — Sonnet 4.6 still accepts them, so nothing should be stripped
        var (chatClient, handler) = await CreateCapabilityClientAsync("claude-sonnet-4-6");

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions { Temperature = 0.5f, TopP = 0.9f });

        // Assert
        var body = LastRequestBody(handler);
        body.ShouldContain("\"temperature\":0.5");
        // The float is serialised at double precision by the SDK, hence the truncated expectation.
        body.ShouldContain("\"top_p\":0.8999999");
    }

    [Fact]
    public async Task CallerWithoutAModelId_StillFiltersAgainstTheBoundModel()
    {
        // The agent runtime builds its ChatOptions without a ModelId, so the model the client was created
        // for is the only signal on that path. This is the case a naive filter gets wrong.
        var (chatClient, handler) = await CreateCapabilityClientAsync("claude-opus-5");

        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions { Temperature = 0.5f });

        LastRequestBody(handler).ShouldNotContain("temperature");
    }

    /// <summary>
    /// Builds the chat client through the capability, which is where the base installs the declaration
    /// filter, with the SDK pointed at a capturing handler.
    /// </summary>
    private static async Task<(IChatClient Client, CapturingHttpMessageHandler Handler)> CreateCapabilityClientAsync(
        string modelId)
    {
        var handler = new CapturingHttpMessageHandler();
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            handler);

        var capability = new AnthropicChatCapability(provider, logger: null);
        var settings = new AnthropicProviderSettings { ApiKey = "test-key" };

        var client = await ((IAIChatCapability)capability)
            .CreateClientAsync(settings, modelId, CancellationToken.None);

        return (client, handler);
    }

    /// <summary>
    /// The body of the last captured request. The capability prefetches the model list before building the
    /// client, so more than one request can reach the handler.
    /// </summary>
    private static string LastRequestBody(CapturingHttpMessageHandler handler)
        => handler.RequestBodies.Count > 0
            ? handler.RequestBodies[^1]
            : throw new InvalidOperationException("No request body was captured.");

    private static async Task SendAndIgnoreFailureAsync(IChatClient chatClient, ChatOptions options)
    {
        try
        {
            await chatClient.GetResponseAsync("hello", options);
        }
        catch (Exception)
        {
            // The capturing handler always fails the request; only the captured body matters here.
        }
    }
}

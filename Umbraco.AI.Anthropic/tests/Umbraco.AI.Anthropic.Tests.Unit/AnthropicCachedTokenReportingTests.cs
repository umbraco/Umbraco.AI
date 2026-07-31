using System.Net;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Pins down that the cache-read token count survives the trip from Anthropic's response to
/// <see cref="UsageDetails.AdditionalCounts"/>, on both the buffered and streamed paths.
/// </summary>
/// <remarks>
/// The SDK's Microsoft.Extensions.AI adapter sums Anthropic's three input figures into
/// <see cref="UsageDetails.InputTokenCount"/> and forwards only the cache <em>write</em> count, dropping the
/// read count that shows caching paying off. These assert the recovery, and the input total's meaning, so a
/// future adapter change surfaces here rather than as a silently empty dashboard figure.
/// </remarks>
public class AnthropicCachedTokenReportingTests
{
    private const long FreshInputTokens = 12;
    private const long CacheWriteTokens = 1500;
    private const long CacheReadTokens = 2048;

    [Fact]
    public async Task NonStreaming_ReportsCacheReadTokens()
    {
        // Arrange
        var chatClient = await CreateClientAsync(new AnthropicApiHandler(NonStreamingBody(CacheReadTokens)));

        // Act
        var response = await chatClient.GetResponseAsync("hello", new ChatOptions { MaxOutputTokens = 64 });

        // Assert
        response.Usage.GetCachedInputTokenCount().ShouldBe(CacheReadTokens);
    }

    [Fact]
    public async Task NonStreaming_LeavesInputTokenCountAsTheTrueTotal()
    {
        // Arrange
        var chatClient = await CreateClientAsync(new AnthropicApiHandler(NonStreamingBody(CacheReadTokens)));

        // Act
        var response = await chatClient.GetResponseAsync("hello", new ChatOptions { MaxOutputTokens = 64 });

        // Assert — the cached figure is a subset of the input total, not an addition to it, which is why the
        // dashboard can show "N input tokens, M of them cached" from these two numbers alone.
        response.Usage?.InputTokenCount.ShouldBe(FreshInputTokens + CacheWriteTokens + CacheReadTokens);
    }

    [Fact]
    public async Task NonStreaming_WhenNothingWasCached_ReportsZeroRatherThanNothing()
    {
        // Arrange — a request that cached nothing still reports the field, so zero is a real answer
        var chatClient = await CreateClientAsync(new AnthropicApiHandler(NonStreamingBody(cacheReadTokens: 0)));

        // Act
        var response = await chatClient.GetResponseAsync("hello", new ChatOptions { MaxOutputTokens = 64 });

        // Assert
        response.Usage.GetCachedInputTokenCount().ShouldBe(0);
    }

    [Fact]
    public async Task NonStreaming_WhenAnthropicOmitsTheField_ReportsNothing()
    {
        // Arrange — no cache figures at all, e.g. caching switched off
        var chatClient = await CreateClientAsync(new AnthropicApiHandler("""
            { "id": "msg_1", "type": "message", "role": "assistant", "model": "claude-opus-5",
              "content": [{"type":"text","text":"hi"}], "stop_reason": "end_turn",
              "usage": { "input_tokens": 12, "output_tokens": 3 } }
            """));

        // Act
        var response = await chatClient.GetResponseAsync("hello", new ChatOptions { MaxOutputTokens = 64 });

        // Assert — null, not zero: "not reported" must stay distinguishable from "nothing was cached"
        response.Usage.GetCachedInputTokenCount().ShouldBeNull();
    }

    [Fact]
    public async Task Streaming_ReportsCacheReadTokensOnTheUsageUpdate()
    {
        // Arrange — the streamed usage update carries no raw representation, so the count has to be carried
        // over from the preceding message_delta event
        var chatClient = await CreateClientAsync(new AnthropicApiHandler(StreamingBody, isStream: true));

        // Act
        var reported = new List<long?>();
        await foreach (var update in chatClient.GetStreamingResponseAsync(
            "hello",
            new ChatOptions { MaxOutputTokens = 64 }))
        {
            foreach (var usage in update.Contents.OfType<UsageContent>())
            {
                reported.Add(usage.Details.GetCachedInputTokenCount());
            }
        }

        // Assert
        reported.ShouldHaveSingleItem().ShouldBe(CacheReadTokens);
    }

    private static string NonStreamingBody(long cacheReadTokens) => $$"""
        {
          "id": "msg_1", "type": "message", "role": "assistant", "model": "claude-opus-5",
          "content": [{"type":"text","text":"hi"}], "stop_reason": "end_turn",
          "usage": {
            "input_tokens": {{FreshInputTokens}},
            "output_tokens": 3,
            "cache_creation_input_tokens": {{CacheWriteTokens}},
            "cache_read_input_tokens": {{cacheReadTokens}}
          }
        }
        """;

    /// <remarks>
    /// Placeholders are substituted rather than interpolated: the event payloads end in runs of closing
    /// braces that a raw interpolated string cannot express unambiguously.
    /// </remarks>
    private static string StreamingBody => """
        event: message_start
        data: {"type":"message_start","message":{"id":"msg_1","type":"message","role":"assistant","model":"claude-opus-5","content":[],"stop_reason":null,"usage":{"input_tokens":FRESH,"output_tokens":1,"cache_creation_input_tokens":WRITE,"cache_read_input_tokens":READ}}}

        event: content_block_start
        data: {"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"hi"}}

        event: content_block_stop
        data: {"type":"content_block_stop","index":0}

        event: message_delta
        data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":FRESH,"output_tokens":3,"cache_creation_input_tokens":WRITE,"cache_read_input_tokens":READ}}

        event: message_stop
        data: {"type":"message_stop"}


        """
        .Replace("FRESH", FreshInputTokens.ToString())
        .Replace("WRITE", CacheWriteTokens.ToString())
        .Replace("READ", CacheReadTokens.ToString());

    private static async Task<IChatClient> CreateClientAsync(AnthropicApiHandler handler)
    {
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            handler);

        IAIChatCapability capability = new AnthropicChatCapability(provider, logger: null);

        return await capability.CreateClientAsync(
            new AnthropicProviderSettings { ApiKey = "test-key" },
            new AnthropicChatCapabilitySettings { PromptCaching = "5m" },
            "claude-opus-5",
            default);
    }

    /// <summary>
    /// Serves a canned successful chat response, and fails the models call so the capability falls back to
    /// inferring support from the model ID — irrelevant here, and one less body to maintain.
    /// </summary>
    private sealed class AnthropicApiHandler(string chatResponse, bool isStream = false) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.Contains("/models", StringComparison.Ordinal) == true)
            {
                throw new HttpRequestException("models endpoint unreachable");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    chatResponse,
                    Encoding.UTF8,
                    isStream ? "text/event-stream" : "application/json"),
            });
        }
    }

    /// <summary>
    /// Guards the key the provider writes and core reads — they are in different assemblies, so a rename on
    /// one side would otherwise only show up as a permanently null column.
    /// </summary>
    [Fact]
    public void CachedInputTokensKey_IsTheWellKnownCoreConstant()
        => Constants.UsageCounts.CachedInputTokens.ShouldBe("Umbraco.AI.CachedInputTokens");
}

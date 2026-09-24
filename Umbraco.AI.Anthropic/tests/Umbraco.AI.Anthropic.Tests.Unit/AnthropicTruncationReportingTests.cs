using System.Net;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Pins down that a response cut off at <c>max_tokens</c> is reported as <see cref="ChatFinishReason.Length"/>
/// on the stream, which is the signal the agent runtime uses to tell the user why a run stopped (#414).
/// </summary>
public class AnthropicTruncationReportingTests
{
    private const string ThinkingCutOffStream = """
        event: message_start
        data: {"type":"message_start","message":{"id":"msg_1","type":"message","role":"assistant","model":"claude-opus-5","content":[],"stop_reason":null,"usage":{"input_tokens":10,"output_tokens":1}}}

        event: content_block_start
        data: {"type":"content_block_start","index":0,"content_block":{"type":"thinking","thinking":"","signature":""}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"thinking_delta","thinking":"Let me think"}}

        event: content_block_stop
        data: {"type":"content_block_stop","index":0}

        event: message_delta
        data: {"type":"message_delta","delta":{"stop_reason":"max_tokens"},"usage":{"output_tokens":1024}}

        event: message_stop
        data: {"type":"message_stop"}


        """;

    [Fact]
    public async Task Streaming_CutOffAtMaxTokens_ReportsLengthFinishReason()
    {
        // Arrange
        var chatClient = await CreateClientAsync();

        // Act
        var finishReasons = new List<ChatFinishReason>();
        await foreach (var update in chatClient.GetStreamingResponseAsync("hello"))
        {
            if (update.FinishReason is { } finishReason)
            {
                finishReasons.Add(finishReason);
            }
        }

        // Assert
        finishReasons.ShouldContain(ChatFinishReason.Length);
    }

    private static Task<IChatClient> CreateClientAsync()
    {
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            new StreamHandler());

        IAIChatCapability capability = new AnthropicChatCapability(provider, logger: null);

        return capability.CreateClientAsync(
            new AnthropicProviderSettings { ApiKey = "test-key" },
            capabilitySettings: null,
            "claude-opus-5",
            default);
    }

    /// <summary>
    /// Serves the canned stream, and fails the models call — the model list is irrelevant here.
    /// </summary>
    private sealed class StreamHandler : HttpMessageHandler
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
                Content = new StringContent(ThinkingCutOffStream, Encoding.UTF8, "text/event-stream"),
            });
        }
    }
}

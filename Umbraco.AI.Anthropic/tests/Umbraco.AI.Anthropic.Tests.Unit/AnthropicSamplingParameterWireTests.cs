using System.Net;
using Anthropic;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// End-to-end checks that the filtered sampling parameters really do not reach the API, asserted against
/// a captured request body rather than the <see cref="ChatOptions"/> handed to the inner client.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AnthropicSamplingParameterChatClientTests"/> covers the filter's own behaviour by recording
/// what it passes down, which is the right shape for testing the decorator. These tests close the gap that
/// approach leaves: the filter is only effective while the SDK's Microsoft.Extensions.AI adapter reads
/// <see cref="ChatOptions.Temperature"/> in the first place. If a future SDK version stopped doing so —
/// as it already does for <see cref="ChatOptions.AdditionalProperties"/>, which it drops entirely — the
/// recording tests would still pass while the parameter quietly stopped being sent at all.
/// </para>
/// <para>
/// Asserting on the serialized request keeps both halves honest: that the filter removes the parameters
/// on a model which rejects them, and that they are genuinely sent on a model which accepts them.
/// </para>
/// </remarks>
public class AnthropicSamplingParameterWireTests
{
    [Fact]
    public async Task ModelRejectingSamplingParameters_TheyDoNotReachTheRequest()
    {
        // Arrange — Opus 5 is past the Claude 4.7 cutoff, so temperature/top_p are rejected there
        var handler = new CapturingHandler();
        var chatClient = CreateFilteredChatClient(handler, "claude-opus-5");

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions { Temperature = 0.5f, TopP = 0.9f });

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("temperature");
        body.ShouldNotContain("top_p");
        body.ShouldContain("hello");
    }

    [Fact]
    public async Task ModelAcceptingSamplingParameters_TheyReachTheRequest()
    {
        // Arrange — Sonnet 4.6 still accepts them, so the filter must leave them alone
        var handler = new CapturingHandler();
        var chatClient = CreateFilteredChatClient(handler, "claude-sonnet-4-6");

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions { Temperature = 0.5f, TopP = 0.9f });

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"temperature\":0.5");
        body.ShouldContain("\"top_p\":0.8999999");
    }

    private static IChatClient CreateFilteredChatClient(CapturingHandler handler, string modelId)
    {
        var inner = new AnthropicClient
        {
            ApiKey = "test-key",
            MaxRetries = 0,
            HttpClient = new HttpClient(handler),
        }.Beta.AsIChatClient(modelId);

        // The same wrapping the chat capability applies.
        return new AnthropicSamplingParameterChatClient(inner, modelId, logger: null);
    }

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

    /// <summary>
    /// Captures the body of every request and short-circuits with an error response, so the test can
    /// assert on what would have gone over the wire without a real API call.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"type":"error","error":{"type":"invalid_request_error","message":"captured"}}"""),
            };
        }
    }
}

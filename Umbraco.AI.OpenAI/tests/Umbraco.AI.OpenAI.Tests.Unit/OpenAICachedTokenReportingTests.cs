using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.OpenAI.Tests.Unit;

/// <summary>
/// Pins down that OpenAI's cached-token figure reaches the count core persists, without this package doing
/// anything to carry it.
/// </summary>
/// <remarks>
/// OpenAI caches eligible prompts automatically, so there is no setting to switch on and no provider-side
/// code involved: the response reports <c>input_tokens_details.cached_tokens</c>, the Microsoft.Extensions.AI
/// adapter puts it on <see cref="UsageDetails.CachedInputTokenCount"/>, and core reads it there. These assert
/// that chain end to end, so an adapter change surfaces here rather than as a silently empty dashboard
/// figure.
/// </remarks>
public class OpenAICachedTokenReportingTests
{
    private const long InputTokens = 3560;
    private const long CachedTokens = 2048;

    [Fact]
    public async Task ReportsCachedTokens()
    {
        // Arrange
        var chatClient = CreateChatClient(new CannedResponseHandler(ResponseBody(CachedTokens)));

        // Act
        var response = await chatClient.GetResponseAsync("hello", new ChatOptions { MaxOutputTokens = 64 });

        // Assert
        response.Usage.GetCachedInputTokenCount().ShouldBe(CachedTokens);
    }

    [Fact]
    public async Task LeavesInputTokenCountAsTheTrueTotal()
    {
        // Arrange
        var chatClient = CreateChatClient(new CannedResponseHandler(ResponseBody(CachedTokens)));

        // Act
        var response = await chatClient.GetResponseAsync("hello", new ChatOptions { MaxOutputTokens = 64 });

        // Assert — OpenAI's cached figure is a subset of the input total, not an addition to it, matching how
        // the dashboard reads the two numbers together
        response.Usage?.InputTokenCount.ShouldBe(InputTokens);
    }

    [Fact]
    public async Task WhenNothingWasCached_ReportsZeroRatherThanNothing()
    {
        // Arrange — a request that cached nothing still reports the field, so zero is a real answer
        var chatClient = CreateChatClient(new CannedResponseHandler(ResponseBody(cachedTokens: 0)));

        // Act
        var response = await chatClient.GetResponseAsync("hello", new ChatOptions { MaxOutputTokens = 64 });

        // Assert
        response.Usage.GetCachedInputTokenCount().ShouldBe(0);
    }

    private static string ResponseBody(long cachedTokens) => $$"""
        {
          "id": "resp_1", "object": "response", "created_at": 1, "status": "completed", "model": "gpt-5.6",
          "output": [
            { "type": "message", "id": "msg_1", "status": "completed", "role": "assistant",
              "content": [{ "type": "output_text", "text": "hi", "annotations": [] }] }
          ],
          "usage": {
            "input_tokens": {{InputTokens}},
            "input_tokens_details": { "cached_tokens": {{cachedTokens}} },
            "output_tokens": 3,
            "output_tokens_details": { "reasoning_tokens": 0 },
            "total_tokens": {{InputTokens + 3}}
          }
        }
        """;

    private static IChatClient CreateChatClient(HttpMessageHandler handler)
        => new OpenAIClient(
                new ApiKeyCredential("test-key"),
                new OpenAIClientOptions
                {
                    Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
                    RetryPolicy = new ClientRetryPolicy(0),
                })
            .GetResponsesClient()
            .AsIChatClient("gpt-5.6");

    /// <summary>
    /// Serves a canned successful response, so a test can assert on what the adapter made of it without a
    /// real API call.
    /// </summary>
    private sealed class CannedResponseHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }
}

using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using Microsoft.Extensions.AI;
using OpenAI;

#pragma warning disable OPENAI001 // The Responses API surface is experimental in the OpenAI SDK

namespace Umbraco.AI.OpenAI.Tests.Unit;

/// <summary>
/// End-to-end checks that the filtered sampling parameters really do not reach the API, asserted against
/// a captured request body rather than the <see cref="ChatOptions"/> handed to the inner client.
/// </summary>
/// <remarks>
/// <see cref="OpenAISamplingParameterChatClientTests"/> covers the filter's own behaviour by recording what
/// it passes down, which is the right shape for testing the decorator. These tests close the gap that
/// approach leaves: the filter is only effective while the Microsoft.Extensions.AI adapter reads
/// <see cref="ChatOptions.Temperature"/> in the first place, and a recording test would stay green if it
/// stopped. Asserting on the serialized request keeps both halves honest — removed on a model that rejects
/// them, genuinely sent on a model that accepts them.
/// </remarks>
public class OpenAISamplingParameterWireTests
{
    [Fact]
    public async Task ModelRejectingSamplingParameters_TheyDoNotReachTheRequest()
    {
        // Arrange — the GPT-5 line restricts the sampling parameters
        var handler = new CapturingHandler();
        var chatClient = CreateFilteredChatClient(handler, "gpt-5.6");

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
        // Arrange — gpt-4o still accepts them, so the filter must leave them alone
        var handler = new CapturingHandler();
        var chatClient = CreateFilteredChatClient(handler, "gpt-4o");

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions { Temperature = 0.5f, TopP = 0.9f });

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"temperature\":0.5");
        body.ShouldContain("\"top_p\":0.9");
    }

    private static IChatClient CreateFilteredChatClient(CapturingHandler handler, string modelId)
    {
        var inner = new OpenAIClient(
                new ApiKeyCredential("test-key"),
                new OpenAIClientOptions
                {
                    Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
                    RetryPolicy = new ClientRetryPolicy(0),
                })
            .GetResponsesClient()
            .AsIChatClient(modelId);

        // The same wrapping the chat capability applies.
        return new OpenAISamplingParameterChatClient(inner, modelId, logger: null);
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
                Content = new StringContent("""{"error":{"message":"captured","type":"invalid_request_error"}}"""),
            };
        }
    }
}

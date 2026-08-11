using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using Microsoft.Extensions.AI;
using OpenAI;

#pragma warning disable OPENAI001 // The Responses API surface is experimental in the OpenAI SDK

namespace Umbraco.AI.OpenAI.Tests.Unit;

/// <summary>
/// Holds the fact the sampling filter depends on: that the Microsoft.Extensions.AI OpenAI adapter reads
/// <see cref="ChatOptions.Temperature"/> and <see cref="ChatOptions.TopP"/> and puts them on the wire.
/// </summary>
/// <remarks>
/// Filtering itself is core's job now, driven by this capability's per-model declaration, and is covered by
/// the core enforcement tests plus an end-to-end wire test on the Anthropic provider (whose SDK client can
/// be redirected to a capturing handler, where OpenAI's is built through a static factory). What remains
/// worth pinning here is the premise: strip these options and the request loses them, which is only true
/// while the adapter reads them at all. A recording test would stay green if it stopped.
/// </remarks>
public class OpenAISamplingParameterWireTests
{
    [Fact]
    public async Task SamplingParameters_AreCarriedToTheRequestByTheAdapter()
    {
        // Arrange — gpt-4o accepts them, and this is the premise the declaration filter relies on
        var handler = new CapturingHandler();
        var chatClient = CreateChatClient(handler, "gpt-4o");

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions { Temperature = 0.5f, TopP = 0.9f });

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"temperature\":0.5");
        body.ShouldContain("\"top_p\":0.9");
    }

    private static IChatClient CreateChatClient(CapturingHandler handler, string modelId)
    {
        return new OpenAIClient(
                new ApiKeyCredential("test-key"),
                new OpenAIClientOptions
                {
                    Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
                    RetryPolicy = new ClientRetryPolicy(0),
                })
            .GetResponsesClient()
            .AsIChatClient(modelId);
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

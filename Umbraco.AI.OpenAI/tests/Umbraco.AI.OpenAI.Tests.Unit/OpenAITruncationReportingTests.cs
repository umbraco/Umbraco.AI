using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Umbraco.AI.OpenAI.Tests.Unit;

/// <summary>
/// Pins down that a streamed response cut off at <c>max_output_tokens</c> is reported as
/// <see cref="ChatFinishReason.Length"/>, which the agent runtime uses to tell the user why a run stopped
/// (#414).
/// </summary>
[Experimental("OPENAI001")]
public class OpenAITruncationReportingTests
{
    private const string IncompleteStream = """
        event: response.created
        data: {"type":"response.created","sequence_number":0,"response":{"id":"resp_1","object":"response","created_at":1,"status":"in_progress","model":"gpt-5.6","output":[]}}

        event: response.output_text.delta
        data: {"type":"response.output_text.delta","sequence_number":1,"item_id":"msg_1","output_index":0,"content_index":0,"delta":"A clipped answ"}

        event: response.incomplete
        data: {"type":"response.incomplete","sequence_number":2,"response":{"id":"resp_1","object":"response","created_at":1,"status":"incomplete","incomplete_details":{"reason":"max_output_tokens"},"model":"gpt-5.6","output":[]}}


        """;

    [Fact]
    public async Task AdapterAlone_DoesNotReportTheLengthFinishReason()
    {
        // Arrange — documents the gap the wrapper exists for; if this starts failing, the adapter now
        // reports it itself and the wrapper can go
        var chatClient = CreateAdapterClient();

        // Act
        var finishReasons = await CollectFinishReasonsAsync(chatClient);

        // Assert
        finishReasons.ShouldNotContain(ChatFinishReason.Length);
    }

    [Fact]
    public async Task Wrapped_CutOffAtMaxOutputTokens_ReportsLengthFinishReason()
    {
        // Arrange
        var chatClient = new OpenAIIncompleteResponseChatClient(CreateAdapterClient());

        // Act
        var finishReasons = await CollectFinishReasonsAsync(chatClient);

        // Assert
        finishReasons.ShouldContain(ChatFinishReason.Length);
    }

    private static async Task<List<ChatFinishReason>> CollectFinishReasonsAsync(IChatClient chatClient)
    {
        var finishReasons = new List<ChatFinishReason>();
        await foreach (var update in chatClient.GetStreamingResponseAsync("hello"))
        {
            if (update.FinishReason is { } finishReason)
            {
                finishReasons.Add(finishReason);
            }
        }

        return finishReasons;
    }

    private static IChatClient CreateAdapterClient()
        => new OpenAIClient(
                new ApiKeyCredential("test-key"),
                new OpenAIClientOptions
                {
                    Transport = new HttpClientPipelineTransport(new HttpClient(new StreamHandler())),
                    RetryPolicy = new ClientRetryPolicy(0),
                })
            .GetResponsesClient()
            .AsIChatClient("gpt-5.6");

    private sealed class StreamHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(IncompleteStream, Encoding.UTF8, "text/event-stream"),
            });
    }
}

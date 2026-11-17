using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using Umbraco.AI.Core;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable OPENAI001 // OpenAI Responses API is experimental in the SDK

namespace Umbraco.AI.MicrosoftFoundry.Tests.Unit;

/// <summary>
/// With "Use Responses API" on, the Foundry provider must not store the response server-side when the caller
/// manages chat history itself. Otherwise the service returns a conversation id and the Agent Framework fails
/// the run ("Only ConversationId or ChatHistoryProvider may be used, but not both"). These tests send a real
/// Responses request through the SDK to a fake transport and read the body, so they pin what goes on the wire.
/// </summary>
public class ClientManagedHistoryChatClientTests
{
    [Fact]
    public async Task Request_IsSentWithoutStorage_WhenHistoryIsClientManaged()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler, clientManagedHistory: true);

        await SendAsync(client);

        handler.RequestBody.ShouldNotBeNull();
        handler.RequestBody.ShouldContain("\"store\":false");
    }

    [Fact]
    public async Task Request_IsUnchanged_WhenHistoryIsNotClientManaged()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler, clientManagedHistory: false);

        await SendAsync(client);

        handler.RequestBody.ShouldNotBeNull();
        handler.RequestBody.ShouldNotContain("\"store\":false");
    }

    [Fact]
    public async Task Request_IsUnchanged_WhenThereIsNoRuntimeContext()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler, context: null);

        await SendAsync(client);

        handler.RequestBody.ShouldNotBeNull();
        handler.RequestBody.ShouldNotContain("\"store\":false");
    }

    [Fact]
    public async Task CallersOptions_AreNotMutated()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler, clientManagedHistory: true);
        var options = new ChatOptions();

        await SendAsync(client, options);

        options.RawRepresentationFactory.ShouldBeNull();
    }

    private static IChatClient CreateClient(CapturingHandler handler, bool clientManagedHistory)
    {
        var context = new AIRuntimeContext([]);
        context.Data[Constants.ContextKeys.ClientManagedChatHistory] = clientManagedHistory;
        return CreateClient(handler, context);
    }

    private static IChatClient CreateClient(CapturingHandler handler, AIRuntimeContext? context)
    {
        var openAi = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential("test-key"),
            new OpenAIClientOptions
            {
                Endpoint = new Uri("https://example.test/openai/v1/"),
                Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
            });

        var inner = openAi.GetResponsesClient().AsIChatClient("gpt-5-mini");
        return new ClientManagedHistoryChatClient(inner, new StubAccessor(context));
    }

    private static async Task SendAsync(IChatClient client, ChatOptions? options = null)
    {
        // The stub reply only has to leave the request body readable; a parse problem on the way back is
        // not what these tests are about.
        try
        {
            await client.GetResponseAsync("hello", options);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
        }
    }

    private sealed class StubAccessor(AIRuntimeContext? context) : IAIRuntimeContextAccessor
    {
        public AIRuntimeContext? Context { get; } = context;
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"id":"resp_1","object":"response","status":"completed","model":"gpt-5-mini","output":[],"usage":{"input_tokens":1,"output_tokens":1,"total_tokens":2}}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }
}

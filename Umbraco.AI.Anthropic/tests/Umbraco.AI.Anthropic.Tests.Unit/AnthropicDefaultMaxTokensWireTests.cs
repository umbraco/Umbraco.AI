using System.Net;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Pins down the <c>max_tokens</c> value sent when a profile leaves Max tokens empty.
/// </summary>
/// <remarks>
/// The SDK adapter's own default is 1024, which a thinking model can spend entirely on reasoning — the
/// response then carries no text and no tool call, and the chat appears to stop for no reason (#414).
/// </remarks>
public class AnthropicDefaultMaxTokensWireTests
{
    private const string ModelsResponse = """
        { "data": [
            { "type": "model", "id": "claude-opus-5", "display_name": "Claude Opus 5",
              "created_at": "2026-06-09T00:00:00Z", "max_input_tokens": 1000000, "max_tokens": 128000,
              "capabilities": { "effort": { "supported": true } } },
            { "type": "model", "id": "claude-small", "display_name": "Claude Small",
              "created_at": "2024-03-07T00:00:00Z", "max_input_tokens": 200000, "max_tokens": 4096 } ],
          "has_more": false }
        """;

    [Fact]
    public async Task NoMaxTokensSet_SendsTheDefaultRatherThanTheAdapters1024()
    {
        // Arrange
        var handler = new AnthropicApiHandler(ModelsResponse);
        var chatClient = await CreateClientAsync(handler, "claude-opus-5", capabilitySettings: null);

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions());

        // Assert
        handler.ChatRequestBodies.ShouldHaveSingleItem()
            .ShouldContain($"\"max_tokens\":{AnthropicChatCapability.DefaultMaxTokens}");
    }

    [Fact]
    public async Task NoMaxTokensSet_ModelLimitBelowTheDefault_SendsTheModelLimit()
    {
        // Arrange
        var handler = new AnthropicApiHandler(ModelsResponse);
        var chatClient = await CreateClientAsync(handler, "claude-small", capabilitySettings: null);

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions());

        // Assert — the API rejects a max_tokens above the model's limit
        handler.ChatRequestBodies.ShouldHaveSingleItem().ShouldContain("\"max_tokens\":4096");
    }

    [Fact]
    public async Task MaxTokensSet_SendsTheConfiguredValue()
    {
        // Arrange
        var handler = new AnthropicApiHandler(ModelsResponse);
        var chatClient = await CreateClientAsync(handler, "claude-opus-5", capabilitySettings: null);

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions { MaxOutputTokens = 500 });

        // Assert
        handler.ChatRequestBodies.ShouldHaveSingleItem().ShouldContain("\"max_tokens\":500");
    }

    [Fact]
    public async Task NoMaxTokensSet_WithEffort_RawRepresentationCarriesTheDefault()
    {
        // Arrange — effort goes through a raw representation, which must supply max_tokens itself
        var handler = new AnthropicApiHandler(ModelsResponse);
        var chatClient = await CreateClientAsync(
            handler,
            "claude-opus-5",
            new AnthropicChatCapabilitySettings { Effort = "high" });

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions());

        // Assert
        var body = handler.ChatRequestBodies.ShouldHaveSingleItem();
        body.ShouldContain($"\"max_tokens\":{AnthropicChatCapability.DefaultMaxTokens}");
        body.ShouldContain("\"effort\":\"high\"");
    }

    [Fact]
    public async Task NoMaxTokensSet_ModelListUnavailable_SendsTheDefault()
    {
        // Arrange — no reported limit to cap against
        var handler = new AnthropicApiHandler(modelsResponse: null);
        var chatClient = await CreateClientAsync(handler, "claude-opus-5", capabilitySettings: null);

        // Act
        await SendAndIgnoreFailureAsync(chatClient, new ChatOptions());

        // Assert
        handler.ChatRequestBodies.ShouldHaveSingleItem()
            .ShouldContain($"\"max_tokens\":{AnthropicChatCapability.DefaultMaxTokens}");
    }

    private static Task<IChatClient> CreateClientAsync(
        AnthropicApiHandler handler,
        string modelId,
        AnthropicChatCapabilitySettings? capabilitySettings)
    {
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            handler);

        IAIChatCapability capability = new AnthropicChatCapability(provider, logger: null);

        return capability.CreateClientAsync(
            new AnthropicProviderSettings { ApiKey = "test-key" },
            capabilitySettings,
            modelId,
            default);
    }

    private static async Task SendAndIgnoreFailureAsync(IChatClient chatClient, ChatOptions options)
    {
        try
        {
            await chatClient.GetResponseAsync("hello", options);
        }
        catch (Exception)
        {
            // The handler always fails the chat request; only the captured body matters here.
        }
    }

    /// <summary>
    /// Stands in for the Anthropic API: serves the models call from a canned response (or fails it, when
    /// none is given) and captures chat request bodies.
    /// </summary>
    private sealed class AnthropicApiHandler(string? modelsResponse) : HttpMessageHandler
    {
        public List<string> ChatRequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.Contains("/models", StringComparison.Ordinal) == true)
            {
                return modelsResponse is null
                    ? throw new HttpRequestException("models endpoint unreachable")
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(modelsResponse, Encoding.UTF8, "application/json"),
                    };
            }

            if (request.Content is not null)
            {
                ChatRequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"type":"error","error":{"type":"invalid_request_error","message":"captured"}}"""),
            };
        }
    }
}

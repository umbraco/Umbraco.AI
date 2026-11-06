using Anthropic.Core;
using Anthropic.Models.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Pins what SDK 13 (API version 2026-10-21) puts on the wire that our provider relies on: the API version
/// header, how the API key is sent, and where system messages end up in the request.
/// </summary>
public class AnthropicWireCompatibilityTests
{
    [Fact]
    public async Task ChatRequest_SendsApiVersion_20261021()
    {
        var (chatClient, handler) = await CreateCapabilityClientAsync();

        await SendAndIgnoreFailureAsync(chatClient, [new(ChatRole.User, "hello")]);

        handler.RequestHeaders[^1]["anthropic-version"].ShouldBe("2026-10-21");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://api.anthropic.com")]
    [InlineData("https://gateway.example.com")]
    public async Task ApiKey_IsSentInTheXApiKeyHeader_NotAsABearerToken(string? endpoint)
    {
        var handler = new CapturingHttpMessageHandler();
        var client = WithHandler(
            AnthropicProvider.CreateAnthropicClient(
                new AnthropicProviderSettings { ApiKey = "secret-key", Endpoint = endpoint }),
            handler);

        await ListModelsIgnoringFailureAsync(client);

        var headers = handler.RequestHeaders.ShouldHaveSingleItem();
        headers["x-api-key"].ShouldBe("secret-key");
        headers.ShouldNotContainKey("authorization");
    }

    [Fact]
    public async Task TrailingSystemMessage_IsFoldedIntoTheTopLevelSystemPrompt()
    {
        // Arrange — the shape the agent runtime produces: stable system message, conversation, then a
        // volatile system message. SDK 13 would otherwise emit the last one as a role "system" message.
        var (chatClient, handler) = await CreateCapabilityClientAsync();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "stable"),
            new(ChatRole.User, "hello"),
            new(ChatRole.System, "volatile"),
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, messages);

        // Assert
        var body = handler.RequestBodies[^1];
        body.ShouldContain("\"system\":[{\"type\":\"text\",\"text\":\"stable\"},{\"type\":\"text\",\"text\":\"volatile\"}]");
        body.ShouldNotContain("\"role\":\"system\"");
    }

    [Fact]
    public async Task TrailingSystemMessage_WhenStreaming_IsFoldedIntoTheTopLevelSystemPrompt()
    {
        var (chatClient, handler) = await CreateCapabilityClientAsync();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "stable"),
            new(ChatRole.User, "hello"),
            new(ChatRole.System, "volatile"),
        };

        try
        {
            await foreach (var _ in chatClient.GetStreamingResponseAsync(messages, new ChatOptions { MaxOutputTokens = 64 }))
            {
            }
        }
        catch (Exception)
        {
            // The capturing handler always fails the request; only the captured request matters here.
        }

        var body = handler.RequestBodies[^1];
        body.ShouldContain("\"system\":[{\"type\":\"text\",\"text\":\"stable\"},{\"type\":\"text\",\"text\":\"volatile\"}]");
        body.ShouldNotContain("\"role\":\"system\"");
    }

    [Fact]
    public async Task ChatRequest_SendsTheKeyInXApiKey_AndNoAuthorizationHeader()
    {
        var (chatClient, handler) = await CreateCapabilityClientAsync();

        await SendAndIgnoreFailureAsync(chatClient, [new(ChatRole.User, "hello")]);

        var headers = handler.RequestHeaders[^1];
        headers["x-api-key"].ShouldBe("test-key");
        headers.ShouldNotContainKey("authorization");
    }

    [Fact]
    public async Task LeadingSystemMessage_IsUnchanged()
    {
        var (chatClient, handler) = await CreateCapabilityClientAsync();

        await SendAndIgnoreFailureAsync(chatClient, [new(ChatRole.System, "stable"), new(ChatRole.User, "hello")]);

        var body = handler.RequestBodies[^1];
        body.ShouldContain("\"system\":[{\"type\":\"text\",\"text\":\"stable\"}]");
        body.ShouldNotContain("\"role\":\"system\"");
    }

    private static global::Anthropic.IAnthropicClient WithHandler(
        global::Anthropic.IAnthropicClient client, CapturingHttpMessageHandler handler)
        => client.WithOptions(o => o with { HttpClient = new HttpClient(handler), MaxRetries = 0 });

    private static async Task ListModelsIgnoringFailureAsync(global::Anthropic.IAnthropicClient client)
    {
        try
        {
            await client.Models.List(new ModelListParams());
        }
        catch (Exception)
        {
            // The capturing handler always fails the request; only what was sent matters here.
        }
    }

    private static async Task<(IChatClient Client, CapturingHttpMessageHandler Handler)> CreateCapabilityClientAsync()
    {
        var handler = new CapturingHttpMessageHandler();
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            handler);

        var client = await ((IAIChatCapability)new AnthropicChatCapability(provider, logger: null))
            .CreateClientAsync(new AnthropicProviderSettings { ApiKey = "test-key" }, "claude-opus-5", CancellationToken.None);

        return (client, handler);
    }

    private static async Task SendAndIgnoreFailureAsync(IChatClient chatClient, IEnumerable<ChatMessage> messages)
    {
        try
        {
            await chatClient.GetResponseAsync(messages, new ChatOptions { MaxOutputTokens = 64 });
        }
        catch (Exception)
        {
            // The capturing handler always fails the request; only the captured request matters here.
        }
    }
}

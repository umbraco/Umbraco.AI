using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Drives the capability's own settings hook end to end — resolved settings in, request body out.
/// </summary>
/// <remarks>
/// <see cref="AnthropicEffortWireTests"/> asserts what the SDK adapter does with a representation built by
/// the test. These tests assert what the representation <em>the capability builds</em> serialises to, which
/// is a different thing and the gap that let a request go out carrying <c>output_config.task_budget: null</c>
/// — rejected by the API as an extra input, because assigning a sibling property marks it present even when
/// its value is null.
/// </remarks>
public class AnthropicApplyEffortWireTests
{
    private const string ModelsResponse = """
        { "data": [ { "type": "model", "id": "claude-opus-5", "display_name": "Claude Opus 5",
                      "created_at": "2026-06-09T00:00:00Z", "max_input_tokens": 1, "max_tokens": 1,
                      "capabilities": { "effort": { "supported": true } } } ],
          "has_more": false }
        """;

    [Fact]
    public async Task ApplyCapabilitySettings_SendsEffortAndNothingElseInOutputConfig()
    {
        // Arrange
        var chatHandler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(chatHandler, effort: "medium");

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert — exactly one key inside output_config
        var body = chatHandler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"output_config\":{\"effort\":\"medium\"}");
        body.ShouldNotContain("task_budget");
        body.ShouldNotContain("format");
    }

    [Fact]
    public async Task ApplyCapabilitySettings_NoEffortConfigured_SendsNoOutputConfig()
    {
        // Arrange
        var chatHandler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(chatHandler, effort: null);

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert
        var body = chatHandler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("output_config");
    }

    [Fact]
    public async Task ApplyCapabilitySettings_LevelTheModelDoesNotAccept_SendsNoOutputConfig()
    {
        // Arrange — xhigh is not offered, so a value stored by an API caller must not be forwarded
        var chatHandler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(chatHandler, effort: "xhigh");

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert
        var body = chatHandler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("output_config");
    }

    private static async Task<IChatClient> CreateConfiguredClientAsync(
        CapturingHttpMessageHandler chatHandler,
        string? effort)
    {
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            new StubHttpMessageHandler(ModelsResponse));

        IAIChatCapability capability = new StubbedChatCapability(provider, chatHandler);
        var settings = new AnthropicProviderSettings { ApiKey = "test-key" };
        var capabilitySettings = effort is null ? new AnthropicChatCapabilitySettings() : new() { Effort = effort };

        return await capability.CreateClientAsync(settings, capabilitySettings, "claude-opus-5", default);
    }

    private static async Task SendAndIgnoreFailureAsync(IChatClient chatClient)
    {
        try
        {
            await chatClient.GetResponseAsync("hello", new ChatOptions { MaxOutputTokens = 64 });
        }
        catch (Exception)
        {
            // The capturing handler always fails the request; only the captured body matters here.
        }
    }

    /// <summary>
    /// The real capability with its chat client pointed at a capturing handler, so the representation it
    /// builds is exercised through the SDK's own serialization.
    /// </summary>
    private sealed class StubbedChatCapability(AnthropicProvider provider, HttpMessageHandler handler)
        : AnthropicChatCapability(provider)
    {
        protected override IChatClient CreateClient(AnthropicProviderSettings settings, string? modelId)
            => new AnthropicClient
            {
                ApiKey = "test-key",
                MaxRetries = 0,
                HttpClient = new HttpClient(handler),
            }.Beta.AsIChatClient(modelId);
    }

    [AIProvider("anthropic", "Anthropic")]
    private sealed class StubbedAnthropicProvider(
        IAIProviderInfrastructure infrastructure,
        IMemoryCache cache,
        HttpMessageHandler handler)
        : AnthropicProvider(infrastructure, cache)
    {
        internal override AnthropicClient CreateModelListClient(AnthropicProviderSettings settings)
            => new()
            {
                ApiKey = "test-key",
                MaxRetries = 0,
                HttpClient = new HttpClient(handler),
            };
    }
}

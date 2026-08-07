using Anthropic;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Drives the prompt-caching setting through the real capability wiring — resolved settings in, request
/// body out.
/// </summary>
/// <remarks>
/// <see cref="BlockLevelCacheControl_IsNotReachable_AdapterAppendsInstructionsAfterOurSystemBlocks"/> is the
/// reason the capability marks the request's last cacheable block via the top-level field instead of the end
/// of the system prompt: it records that the adapter appends the caller's instructions <em>after</em> any
/// system blocks a raw representation supplies. If a future SDK changes that, it fails here rather than the
/// marker silently caching nothing.
/// </remarks>
public class AnthropicPromptCachingWireTests
{
    [Theory]
    [InlineData("5m", "\"cache_control\":{\"type\":\"ephemeral\",\"ttl\":\"5m\"}")]
    [InlineData("1h", "\"cache_control\":{\"type\":\"ephemeral\",\"ttl\":\"1h\"}")]
    public async Task PromptCaching_SetsTopLevelCacheControl(string setting, string expected)
    {
        // Arrange
        var handler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(
            handler,
            new AnthropicChatCapabilitySettings { PromptCaching = setting });

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain(expected);
    }

    [Fact]
    public async Task PromptCaching_WhenUnset_SendsNoCacheControl()
    {
        // Arrange
        var handler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(handler, new AnthropicChatCapabilitySettings());

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("cache_control");
    }

    [Fact]
    public async Task PromptCaching_WithUnrecognisedValue_SendsNoCacheControl()
    {
        // Arrange — e.g. a hand-edited profile, or a TTL Anthropic has not shipped
        var handler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(
            handler,
            new AnthropicChatCapabilitySettings { PromptCaching = "30m" });

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert — dropped rather than forwarded, so the request is not failed by a stale stored value
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("cache_control");
    }

    [Fact]
    public async Task PromptCachingAndEffort_Together_BothReachTheWire()
    {
        // Arrange — the two settings share one raw representation, so neither may clobber the other
        var handler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(
            handler,
            new AnthropicChatCapabilitySettings { Effort = "medium", PromptCaching = "5m" });

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"output_config\":{\"effort\":\"medium\"}");
        body.ShouldContain("\"cache_control\":{\"type\":\"ephemeral\",\"ttl\":\"5m\"}");
        body.ShouldContain("\"model\":\"claude-opus-5\"");
    }

    [Fact]
    public async Task PromptCaching_LeavesTheAdapterSuppliedPromptAndToolsIntact()
    {
        // Arrange — the cached prefix is the system prompt and tools, so they must still be there
        var handler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(
            handler,
            new AnthropicChatCapabilitySettings { PromptCaching = "5m" });

        var options = new ChatOptions
        {
            MaxOutputTokens = 64,
            Instructions = "be terse",
            Tools = [AIFunctionFactory.Create(() => "ok", "ping", "pings")],
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, options);

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"cache_control\":{\"type\":\"ephemeral\",\"ttl\":\"5m\"}");
        body.ShouldContain("be terse");
        body.ShouldContain("\"name\":\"ping\"");
    }

    [Fact]
    public async Task BlockLevelCacheControl_IsNotReachable_AdapterAppendsInstructionsAfterOurSystemBlocks()
    {
        // Arrange — the rejected alternative: mark the last block of the system prompt ourselves
        var handler = new CapturingHttpMessageHandler();
        var chatClient = new AnthropicClient
        {
            ApiKey = "test-key",
            MaxRetries = 0,
            HttpClient = new HttpClient(handler),
        }.Beta.AsIChatClient("claude-opus-5");

        var options = new ChatOptions
        {
            MaxOutputTokens = 64,
            Instructions = "the real system prompt",
            RawRepresentationFactory = _ => new MessageCreateParams
            {
                Model = "claude-opus-5",
                MaxTokens = 64,
                Messages = [],
                System = new List<BetaTextBlockParam>
                {
                    new() { Text = "marker", CacheControl = new BetaCacheControlEphemeral() },
                },
            },
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, options);

        // Assert — the marked block lands first and the real prompt is appended after it, so a block-level
        // breakpoint would cache only the marker. Hence the top-level field.
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain(
            "\"system\":[{\"type\":\"text\",\"text\":\"marker\",\"cache_control\":{\"type\":\"ephemeral\"}},"
            + "{\"type\":\"text\",\"text\":\"the real system prompt\"}]");
    }

    /// <summary>
    /// Builds the client the way production does: the capability-settings decorator over the declaration
    /// filter over the SDK client, with the settings under test baked in.
    /// </summary>
    private static async Task<IChatClient> CreateConfiguredClientAsync(
        CapturingHttpMessageHandler handler,
        AnthropicChatCapabilitySettings capabilitySettings)
    {
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            handler);

        IAIChatCapability capability = new AnthropicChatCapability(provider, logger: null);

        return await capability.CreateClientAsync(
            new AnthropicProviderSettings { ApiKey = "test-key" },
            capabilitySettings,
            "claude-opus-5",
            default);
    }

    private static async Task SendAndIgnoreFailureAsync(IChatClient chatClient, ChatOptions? options = null)
    {
        try
        {
            await chatClient.GetResponseAsync("hello", options ?? new ChatOptions { MaxOutputTokens = 64 });
        }
        catch (Exception)
        {
            // The capturing handler always fails the request; only the captured body matters here.
        }
    }
}

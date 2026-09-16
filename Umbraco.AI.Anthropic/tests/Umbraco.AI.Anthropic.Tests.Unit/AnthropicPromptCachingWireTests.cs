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
/// <see cref="BlockLevelCacheControl_IsNotReachable_AdapterAppendsInstructionsAfterOurSystemBlocks"/> records
/// why the capability cannot place a block-level marker via <see cref="ChatOptions.RawRepresentationFactory"/>
/// itself: the adapter appends the caller's instructions <em>after</em> any system blocks a raw
/// representation supplies, so a marker placed there would sit ahead of the content worth caching. If a
/// future SDK changes that, it fails here rather than the marker silently caching nothing. That constraint is
/// exactly why the block-level marker (umbraco/Umbraco.AI#382) is instead applied by
/// <c>AnthropicSystemBlockCacheMarkingChatClient</c> mutating the message list, covered separately below.
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

    [Fact]
    public async Task PromptCaching_MarksTheLastSystemMessage_WithBlockLevelCacheControl()
    {
        // Arrange — a system-role message simulates what AIAgentSystemMessageChatClient now puts first
        // (agent instructions + context resources), with the volatile runtime-context prompt appended after
        // via Instructions (umbraco/Umbraco.AI#382).
        var handler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(
            handler, new AnthropicChatCapabilitySettings { PromptCaching = "1h" });

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "stable instructions and context resources"),
            new(ChatRole.User, "hello"),
        };
        var options = new ChatOptions { MaxOutputTokens = 64, Instructions = "volatile entity context" };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, messages, options);

        // Assert — the stable system block gets its own block-level breakpoint, positioned before the
        // volatile Instructions block, which stays unmarked; the top-level field (covering the conversation
        // tail) is still set too, so both breakpoints coexist.
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain(
            "\"system\":[{\"type\":\"text\",\"text\":\"stable instructions and context resources\","
            + "\"cache_control\":{\"type\":\"ephemeral\",\"ttl\":\"1h\"}},"
            + "{\"type\":\"text\",\"text\":\"volatile entity context\"}]");
        body.ShouldContain("\"cache_control\":{\"type\":\"ephemeral\",\"ttl\":\"1h\"}");
    }

    [Fact]
    public async Task PromptCaching_WhenUnset_DoesNotMarkTheSystemMessage()
    {
        // Arrange — a system-role message is present, but caching is off, so it must stay unmarked
        var handler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(handler, new AnthropicChatCapabilitySettings());

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "stable instructions"),
            new(ChatRole.User, "hello"),
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, messages);

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("cache_control");
        body.ShouldContain("\"text\":\"stable instructions\"");
    }

    [Fact]
    public async Task PromptCaching_WithNoSystemRoleMessage_StillSetsOnlyTheTopLevelMarker()
    {
        // Arrange — nothing for the block-level marker to attach to; must not throw
        var handler = new CapturingHttpMessageHandler();
        var chatClient = await CreateConfiguredClientAsync(
            handler, new AnthropicChatCapabilitySettings { PromptCaching = "5m" });

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert — exactly one cache_control marker (the top-level field), not a second block-level one
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.Split("cache_control").Length.ShouldBe(2); // one occurrence splits into 2 parts
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

    private static async Task SendAndIgnoreFailureAsync(
        IChatClient chatClient, IEnumerable<ChatMessage> messages, ChatOptions? options = null)
    {
        try
        {
            await chatClient.GetResponseAsync(messages, options ?? new ChatOptions { MaxOutputTokens = 64 });
        }
        catch (Exception)
        {
            // The capturing handler always fails the request; only the captured body matters here.
        }
    }
}

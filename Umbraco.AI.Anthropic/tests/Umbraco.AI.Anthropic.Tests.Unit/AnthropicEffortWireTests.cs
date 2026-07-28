using Anthropic;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.AI;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Pins down how a provider-declared setting reaches the Anthropic wire, asserted against a captured
/// request body rather than a live call.
/// </summary>
/// <remarks>
/// These are the constraints <c>AnthropicChatCapability.ApplyCapabilitySettings</c> is written against:
/// <see cref="ChatOptions.AdditionalProperties"/> never arrives, and
/// <see cref="ChatOptions.RawRepresentationFactory"/> does but takes precedence over the options the
/// adapter would otherwise apply. If a future SDK version changes either, these fail rather than the
/// setting silently going missing.
/// </remarks>
public class AnthropicEffortWireTests
{
    [Fact]
    public async Task RawRepresentationFactory_SetsOutputConfigEffort_ReachesTheRequestBody()
    {
        // Arrange
        var handler = new CapturingHttpMessageHandler();
        var chatClient = CreateChatClient(handler, "claude-opus-5");

        var options = new ChatOptions
        {
            MaxOutputTokens = 64,
            RawRepresentationFactory = _ => new MessageCreateParams
            {
                Model = "claude-opus-5",
                MaxTokens = 64,
                Messages = [],
                OutputConfig = new BetaOutputConfig { Effort = Effort.Medium },
            },
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, options);

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"output_config\":{\"effort\":\"medium\"}");
        body.ShouldContain("\"model\":\"claude-opus-5\"");
        body.ShouldContain("\"max_tokens\":64");
        // The adapter supplies the conversation even though the representation carried no messages.
        body.ShouldContain("hello");
    }

    [Fact]
    public async Task RawRepresentationFactory_TakesPrecedenceOverChatOptions()
    {
        // Arrange — deliberately mismatched values, to show which side wins
        var handler = new CapturingHttpMessageHandler();
        var chatClient = CreateChatClient(handler, "claude-opus-5");

        var options = new ChatOptions
        {
            MaxOutputTokens = 4096,
            RawRepresentationFactory = _ => new MessageCreateParams
            {
                Model = "from-representation",
                MaxTokens = 7,
                Messages = [],
            },
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, options);

        // Assert — this is why the capability carries the model and max tokens into the representation
        // itself: the adapter does not put them back.
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"model\":\"from-representation\"");
        body.ShouldContain("\"max_tokens\":7");
        body.ShouldNotContain("4096");
    }

    [Fact]
    public async Task AdditionalProperties_AreDroppedBeforeTheRequestIsBuilt()
    {
        // Arrange
        var handler = new CapturingHttpMessageHandler();
        var chatClient = CreateChatClient(handler, "claude-sonnet-4-6");

        var options = new ChatOptions
        {
            MaxOutputTokens = 64,
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["thinking"] = 2048,
                ["output_config"] = new Dictionary<string, object?> { ["effort"] = "medium" },
            },
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, options);

        // Assert — nothing from AdditionalProperties survives, so it is not a usable channel for
        // provider-specific request options.
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("thinking");
        body.ShouldNotContain("output_config");
    }

    private static IChatClient CreateChatClient(CapturingHttpMessageHandler handler, string modelId)
        => new AnthropicClient
        {
            ApiKey = "test-key",
            MaxRetries = 0,
            HttpClient = new HttpClient(handler),
        }.Beta.AsIChatClient(modelId);

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

    [Fact]
    public async Task RawRepresentationFactory_DoesNotSuppressStandardChatOptions()
    {
        // Arrange — a representation carrying only the provider setting, alongside standard options
        var handler = new CapturingHttpMessageHandler();
        var chatClient = CreateChatClient(handler, "claude-opus-5");

        var options = new ChatOptions
        {
            Temperature = 0.5f,
            TopP = 0.9f,
            MaxOutputTokens = 128,
            Instructions = "be terse",
            RawRepresentationFactory = _ => new MessageCreateParams
            {
                Model = "claude-opus-5",
                MaxTokens = 128,
                Messages = [],
                OutputConfig = new BetaOutputConfig { Effort = Effort.Medium },
            },
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, options);

        // Assert — everything except model and max tokens is still applied from ChatOptions, so setting a
        // provider setting neither drops the profile's temperature nor bypasses a decorator that filters
        // the sampling parameters for models which reject them (see #265).
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"output_config\":{\"effort\":\"medium\"}");
        body.ShouldContain("\"temperature\":0.5");
        body.ShouldContain("\"top_p\":0.8999999");
        body.ShouldContain("be terse");
    }

    [Fact]
    public async Task SamplingFilterAndEffort_Together_BothTakeEffect()
    {
        // Arrange — the shape the chat capability actually builds: the capability-settings client wraps the
        // sampling-parameter filter, which clones ChatOptions. If that clone dropped the raw representation
        // the effort would vanish whenever a model rejects temperature, which is every effort-capable model.
        var handler = new CapturingHttpMessageHandler();
        var inner = new AnthropicClient
        {
            ApiKey = "test-key",
            MaxRetries = 0,
            HttpClient = new HttpClient(handler),
        }.Beta.AsIChatClient("claude-opus-5");

        var chatClient = new AnthropicSamplingParameterChatClient(inner, "claude-opus-5", logger: null);

        var options = new ChatOptions
        {
            Temperature = 0.5f,
            MaxOutputTokens = 128,
            RawRepresentationFactory = _ => new MessageCreateParams
            {
                Model = "claude-opus-5",
                MaxTokens = 128,
                Messages = [],
                OutputConfig = new BetaOutputConfig { Effort = Effort.Medium },
            },
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, options);

        // Assert — effort sent, temperature filtered out
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"output_config\":{\"effort\":\"medium\"}");
        body.ShouldNotContain("temperature");
    }
}

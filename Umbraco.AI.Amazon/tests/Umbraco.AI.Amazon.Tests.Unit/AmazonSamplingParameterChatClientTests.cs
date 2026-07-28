using Microsoft.Extensions.AI;
using Umbraco.AI.Amazon.Tests.Unit.Fakes;

namespace Umbraco.AI.Amazon.Tests.Unit;

/// <summary>
/// Covers the sampling-parameter filtering for Bedrock, which inherits its restrictions from whichever
/// vendor built the model it is fronting (issue #256 follow-up).
/// </summary>
public class AmazonSamplingParameterChatClientTests
{
    private static (AmazonSamplingParameterChatClient Client, RecordingChatClient Inner) CreateClient(
        string? boundModelId)
    {
        var inner = new RecordingChatClient();
        return (new AmazonSamplingParameterChatClient(inner, boundModelId, logger: null), inner);
    }

    private static readonly List<ChatMessage> Messages = [new(ChatRole.User, "hi")];

    [Theory]
    // Amazon's own models, plus Mistral and Meta, all accept the sampling parameters.
    [InlineData("amazon.nova-pro-v1:0")]
    [InlineData("us.amazon.nova-lite-v1:0")]
    [InlineData("mistral.mistral-large-2407-v1:0")]
    [InlineData("meta.llama3-1-70b-instruct-v1:0")]
    // Bedrock-hosted Claude families that still accept them.
    [InlineData("anthropic.claude-3-5-sonnet-20240620-v1:0")]
    [InlineData("anthropic.claude-3-haiku-20240307-v1:0")]
    [InlineData("anthropic.claude-sonnet-4-20250514-v1:0")]
    [InlineData("us.anthropic.claude-opus-4-5-20251101-v1:0")]
    [InlineData("eu.anthropic.claude-sonnet-4-6-v1:0")]
    public async Task GetResponseAsync_ModelSupportsSampling_ForwardsTemperature(string modelId)
    {
        var (client, inner) = CreateClient(modelId);
        var options = new ChatOptions { Temperature = 0.3f, MaxOutputTokens = 4096 };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions.ShouldNotBeNull();
        inner.ReceivedOptions.Temperature.ShouldBe(0.3f);
        inner.ReceivedOptions.MaxOutputTokens.ShouldBe(4096);
    }

    [Theory]
    // Bedrock-hosted Claude inherits Anthropic's removal of the sampling parameters from 4.7 onwards.
    [InlineData("anthropic.claude-opus-4-7-v1:0")]
    [InlineData("anthropic.claude-opus-4-8-v1:0")]
    [InlineData("us.anthropic.claude-opus-4-8-v1:0")]
    [InlineData("apac.anthropic.claude-sonnet-5-v1:0")]
    public async Task GetResponseAsync_ModelRejectsSampling_DropsSamplingParameters(string modelId)
    {
        var (client, inner) = CreateClient(modelId);
        var options = new ChatOptions { Temperature = 0.3f, TopP = 0.9f, TopK = 40, MaxOutputTokens = 4096 };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions.ShouldNotBeNull();
        inner.ReceivedOptions.Temperature.ShouldBeNull();
        inner.ReceivedOptions.TopP.ShouldBeNull();
        inner.ReceivedOptions.TopK.ShouldBeNull();

        // MaxOutputTokens is accepted by every model and is the setting #256 was actually about —
        // filtering must not take it with the sampling parameters.
        inner.ReceivedOptions.MaxOutputTokens.ShouldBe(4096);
    }

    [Fact]
    public async Task GetResponseAsync_UnknownVendor_DropsSamplingParameters()
    {
        // A vendor we don't enumerate fails safe, rather than assuming support.
        var (client, inner) = CreateClient("cohere.command-r-plus-v1:0");
        var options = new ChatOptions { Temperature = 0.3f };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_OptionsModelIdOverridesBoundModel()
    {
        // A caller-supplied ModelId wins over the model the client was bound to.
        var (client, inner) = CreateClient("amazon.nova-pro-v1:0");
        var options = new ChatOptions { ModelId = "anthropic.claude-opus-4-8-v1:0", Temperature = 0.3f };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_NullOptions_StaysNull()
    {
        var (client, inner) = CreateClient("anthropic.claude-opus-4-8-v1:0");

        await client.GetResponseAsync(Messages);

        inner.WasCalled.ShouldBeTrue();
        inner.ReceivedOptions.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_DoesNotMutateCallerOptions()
    {
        var (client, _) = CreateClient("anthropic.claude-opus-4-8-v1:0");
        var options = new ChatOptions { Temperature = 0.3f, TopP = 0.9f, TopK = 40 };

        await client.GetResponseAsync(Messages, options);

        options.Temperature.ShouldBe(0.3f);
        options.TopP.ShouldBe(0.9f);
        options.TopK.ShouldBe(40);
    }

    [Fact]
    public async Task GetResponseAsync_NoSamplingParametersSet_PassesOriginalInstanceThrough()
    {
        var (client, inner) = CreateClient("anthropic.claude-opus-4-8-v1:0");
        var options = new ChatOptions { MaxOutputTokens = 1024 };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions.ShouldBeSameAs(options);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_ModelRejectsSampling_DropsSamplingParameters()
    {
        var (client, inner) = CreateClient("anthropic.claude-opus-4-8-v1:0");
        var options = new ChatOptions { Temperature = 0.3f };

        await foreach (var _ in client.GetStreamingResponseAsync(Messages, options))
        {
            // drain
        }

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }
}

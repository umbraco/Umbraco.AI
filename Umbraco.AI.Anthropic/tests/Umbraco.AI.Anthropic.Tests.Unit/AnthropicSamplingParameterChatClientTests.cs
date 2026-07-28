using Microsoft.Extensions.AI;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Covers the sampling-parameter filtering that keeps a profile's configured temperature from being sent
/// to Claude models that reject it (issue #256 follow-up).
/// </summary>
public class AnthropicSamplingParameterChatClientTests
{
    private static (AnthropicSamplingParameterChatClient Client, RecordingChatClient Inner) CreateClient(
        string? boundModelId)
    {
        var inner = new RecordingChatClient();
        return (new AnthropicSamplingParameterChatClient(inner, boundModelId, logger: null), inner);
    }

    private static readonly List<ChatMessage> Messages = [new(ChatRole.User, "hi")];

    [Theory]
    // Models that still accept the sampling parameters.
    [InlineData("claude-3-opus-20240229")]
    [InlineData("claude-3-5-sonnet-20241022")]
    [InlineData("claude-3-7-sonnet-20250219")]
    [InlineData("claude-sonnet-4-20250514")]
    [InlineData("claude-opus-4-1-20250805")]
    [InlineData("claude-opus-4-5-20251101")]
    [InlineData("claude-haiku-4-5-20251001")]
    [InlineData("claude-sonnet-4-6")]
    [InlineData("claude-opus-4-6")]
    public async Task GetResponseAsync_ModelSupportsSampling_ForwardsTemperature(string modelId)
    {
        var (client, inner) = CreateClient(modelId);
        var options = new ChatOptions { Temperature = 0.3f, MaxOutputTokens = 64000 };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions.ShouldNotBeNull();
        inner.ReceivedOptions.Temperature.ShouldBe(0.3f);
        inner.ReceivedOptions.MaxOutputTokens.ShouldBe(64000);
    }

    [Theory]
    // Anthropic removed the sampling parameters from Opus 4.7 onwards.
    [InlineData("claude-opus-4-7")]
    [InlineData("claude-opus-4-8")]
    [InlineData("claude-opus-5")]
    [InlineData("claude-sonnet-5")]
    public async Task GetResponseAsync_ModelRejectsSampling_DropsSamplingParameters(string modelId)
    {
        var (client, inner) = CreateClient(modelId);
        var options = new ChatOptions { Temperature = 0.3f, TopP = 0.9f, TopK = 40, MaxOutputTokens = 64000 };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions.ShouldNotBeNull();
        inner.ReceivedOptions.Temperature.ShouldBeNull();
        inner.ReceivedOptions.TopP.ShouldBeNull();
        inner.ReceivedOptions.TopK.ShouldBeNull();

        // MaxOutputTokens is accepted by every model and is the setting #256 was actually about —
        // filtering must not take it with the sampling parameters.
        inner.ReceivedOptions.MaxOutputTokens.ShouldBe(64000);
    }

    [Fact]
    public async Task GetResponseAsync_UnknownModel_DropsSamplingParameters()
    {
        // Unknown models fail safe: dropping a value that would have worked is a degraded request,
        // whereas sending one that is rejected is a failed request.
        var (client, inner) = CreateClient("claude-something-not-yet-released");
        var options = new ChatOptions { Temperature = 0.3f };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_NoModelResolved_DropsSamplingParameters()
    {
        var (client, inner) = CreateClient(boundModelId: null);
        var options = new ChatOptions { Temperature = 0.3f };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_OptionsModelIdOverridesBoundModel()
    {
        // A caller-supplied ModelId wins over the model the client was bound to.
        var (client, inner) = CreateClient("claude-sonnet-4-6");
        var options = new ChatOptions { ModelId = "claude-opus-4-8", Temperature = 0.3f };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_BoundModelUsedWhenOptionsHaveNoModelId()
    {
        // The agent runtime builds ChatOptions without a ModelId, so the bound model is the only way to
        // identify the target — this is the path issue #256's follow-up was reported on.
        var (client, inner) = CreateClient("claude-opus-4-8");
        var options = new ChatOptions { Temperature = 0.3f };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_DoesNotMutateCallerOptions()
    {
        var (client, _) = CreateClient("claude-opus-4-8");
        var options = new ChatOptions { Temperature = 0.3f, TopP = 0.9f, TopK = 40 };

        await client.GetResponseAsync(Messages, options);

        options.Temperature.ShouldBe(0.3f);
        options.TopP.ShouldBe(0.9f);
        options.TopK.ShouldBe(40);
    }

    [Fact]
    public async Task GetResponseAsync_NoSamplingParametersSet_PassesOriginalInstanceThrough()
    {
        // Nothing to remove, so no clone should be taken.
        var (client, inner) = CreateClient("claude-opus-4-8");
        var options = new ChatOptions { MaxOutputTokens = 1024 };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions.ShouldBeSameAs(options);
    }

    [Fact]
    public async Task GetResponseAsync_NullOptions_StaysNull()
    {
        var (client, inner) = CreateClient("claude-opus-4-8");

        await client.GetResponseAsync(Messages);

        inner.WasCalled.ShouldBeTrue();
        inner.ReceivedOptions.ShouldBeNull();
    }

    [Fact]
    public async Task GetStreamingResponseAsync_ModelRejectsSampling_DropsSamplingParameters()
    {
        var (client, inner) = CreateClient("claude-opus-4-8");
        var options = new ChatOptions { Temperature = 0.3f };

        await foreach (var _ in client.GetStreamingResponseAsync(Messages, options))
        {
            // drain
        }

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }
}

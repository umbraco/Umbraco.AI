using Microsoft.Extensions.AI;
using Umbraco.AI.OpenAI.Tests.Unit.Fakes;

namespace Umbraco.AI.OpenAI.Tests.Unit;

/// <summary>
/// Covers the sampling-parameter filtering that keeps a profile's configured temperature from being sent
/// to OpenAI reasoning models, which restrict it (issue #256 follow-up).
/// </summary>
public class OpenAISamplingParameterChatClientTests
{
    private static (OpenAISamplingParameterChatClient Client, RecordingChatClient Inner) CreateClient(
        string? boundModelId)
    {
        var inner = new RecordingChatClient();
        return (new OpenAISamplingParameterChatClient(inner, boundModelId, logger: null), inner);
    }

    private static readonly List<ChatMessage> Messages = [new(ChatRole.User, "hi")];

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-4-turbo")]
    [InlineData("gpt-4.1")]
    [InlineData("gpt-3.5-turbo")]
    [InlineData("chatgpt-4o-latest")]
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
    // Reasoning models restrict the sampling parameters.
    [InlineData("o1")]
    [InlineData("o1-mini")]
    [InlineData("o3")]
    [InlineData("o3-mini")]
    [InlineData("gpt-5")]
    [InlineData("gpt-5-mini")]
    public async Task GetResponseAsync_ModelRejectsSampling_DropsSamplingParameters(string modelId)
    {
        var (client, inner) = CreateClient(modelId);
        var options = new ChatOptions { Temperature = 0.3f, TopP = 0.9f, TopK = 40, MaxOutputTokens = 4096 };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions.ShouldNotBeNull();
        inner.ReceivedOptions.Temperature.ShouldBeNull();
        inner.ReceivedOptions.TopP.ShouldBeNull();
        inner.ReceivedOptions.TopK.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_ModelRejectsSampling_PreservesMaxOutputTokens()
    {
        var (client, inner) = CreateClient("o3");
        var options = new ChatOptions { Temperature = 0.3f, MaxOutputTokens = 4096 };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions!.MaxOutputTokens.ShouldBe(4096);
    }

    [Fact]
    public async Task GetResponseAsync_UnknownModel_DropsSamplingParameters()
    {
        // Unknown models fail safe: dropping a value that would have worked is a degraded request,
        // whereas sending one that is rejected is a failed request.
        var (client, inner) = CreateClient("some-future-model");
        var options = new ChatOptions { Temperature = 0.3f };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_BoundModelUsedWhenOptionsHaveNoModelId()
    {
        // The agent runtime builds ChatOptions without a ModelId, so the bound model is the only way to
        // identify the target.
        var (client, inner) = CreateClient("o3");
        var options = new ChatOptions { Temperature = 0.3f };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_OptionsModelIdOverridesBoundModel()
    {
        var (client, inner) = CreateClient("gpt-4o");
        var options = new ChatOptions { ModelId = "o3", Temperature = 0.3f };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }

    [Fact]
    public async Task GetResponseAsync_DoesNotMutateCallerOptions()
    {
        var (client, _) = CreateClient("o3");
        var options = new ChatOptions { Temperature = 0.3f, TopP = 0.9f, TopK = 40 };

        await client.GetResponseAsync(Messages, options);

        options.Temperature.ShouldBe(0.3f);
        options.TopP.ShouldBe(0.9f);
        options.TopK.ShouldBe(40);
    }

    [Fact]
    public async Task GetResponseAsync_NoSamplingParametersSet_PassesOriginalInstanceThrough()
    {
        var (client, inner) = CreateClient("o3");
        var options = new ChatOptions { MaxOutputTokens = 1024 };

        await client.GetResponseAsync(Messages, options);

        inner.ReceivedOptions.ShouldBeSameAs(options);
    }

    [Fact]
    public async Task GetResponseAsync_NullOptions_StaysNull()
    {
        var (client, inner) = CreateClient("o3");

        await client.GetResponseAsync(Messages);

        inner.WasCalled.ShouldBeTrue();
        inner.ReceivedOptions.ShouldBeNull();
    }

    [Fact]
    public async Task GetStreamingResponseAsync_ModelRejectsSampling_DropsSamplingParameters()
    {
        var (client, inner) = CreateClient("o3");
        var options = new ChatOptions { Temperature = 0.3f };

        await foreach (var _ in client.GetStreamingResponseAsync(Messages, options))
        {
            // drain
        }

        inner.ReceivedOptions!.Temperature.ShouldBeNull();
    }
}

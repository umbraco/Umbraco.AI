using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Telemetry;

namespace Umbraco.AI.Tests.Unit.Middleware;

/// <summary>
/// #562: a streamed chat call's gen_ai span carries the tracked call's tags, like a non-streamed one.
/// </summary>
public class AIOpenTelemetryChatMiddlewareTagsTests
{
    [Fact]
    public async Task StreamedCall_TagsTheGenAiSpan()
    {
        // Arrange
        Activity? span = null;
        var client = new AIOpenTelemetryChatMiddleware(NullLoggerFactory.Instance)
            .Apply(new StreamingClient(() => span = Activity.Current));
        var tags = new Dictionary<string, string> { [AITelemetry.Tags.ProfileAlias] = "streamed-profile" };
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AITelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        // Act: as the tracking client does, enter the call's scope around each step of the stream.
        await using var updates = client.GetStreamingResponseAsync("hi").GetAsyncEnumerator();
        while (true)
        {
            using (AITraceTags.Enter(tags))
            {
                if (!await updates.MoveNextAsync())
                {
                    break;
                }
            }
        }

        // Assert
        span.ShouldNotBeNull();
        span.Source.Name.ShouldBe(AITelemetry.SourceName);
        span.GetTagItem(AITelemetry.Tags.ProfileAlias).ShouldBe("streamed-profile");
    }

    [Fact]
    public void NestedCallWithNoTags_DoesNotShowItsParentsTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AITelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource(AITelemetry.SourceName);

        // Act
        using (AITraceTags.Enter(new Dictionary<string, string> { [AITelemetry.Tags.ProfileAlias] = "parent" }))
        using (AITraceTags.Enter(new Dictionary<string, string>()))
        {
            using var span = source.StartActivity("gen_ai.chat");
            AITraceTags.Apply(span);

            // Assert
            span.ShouldNotBeNull();
            span.GetTagItem(AITelemetry.Tags.ProfileAlias).ShouldBeNull();
        }
    }

    [Fact]
    public async Task StreamedCall_StoppedEarly_DisposesTheProviderStream()
    {
        // Arrange
        var inner = new StreamingClient(() => { });
        var client = new AIOpenTelemetryChatMiddleware(NullLoggerFactory.Instance).Apply(inner);

        // Act: read one update, then stop without cancelling (e.g. a guardrail blocks mid-stream).
        await foreach (var _ in client.GetStreamingResponseAsync("hi"))
        {
            break;
        }

        // Assert
        inner.Disposed.ShouldBeTrue();
    }

    private sealed class StreamingClient(Action onCall) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            onCall();
            try
            {
                await Task.Yield();
                yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
                yield return new ChatResponseUpdate(ChatRole.Assistant, "more");
            }
            finally
            {
                Disposed = true;
            }
        }

        public bool Disposed { get; private set; }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}

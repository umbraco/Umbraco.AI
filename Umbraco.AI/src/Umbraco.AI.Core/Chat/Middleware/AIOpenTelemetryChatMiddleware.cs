using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Telemetry;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Chat middleware that adds OpenTelemetry tracing and metrics using M.E.AI's built-in
/// <see cref="OpenTelemetryChatClient"/>. Emits <c>gen_ai.*</c> semantic convention spans
/// and metrics (operation duration, token usage, streaming latency).
/// </summary>
/// <remarks>
/// <para>
/// This middleware has zero overhead when no OpenTelemetry listener is configured.
/// It is registered as the innermost middleware, so its span covers just the provider call. The tracked
/// call's <c>umbraco.ai.*</c> tags are put on that span here, since the tracking middleware runs before
/// the span exists (#562).
/// </para>
/// <para>
/// Users opt in to collecting telemetry by adding the source name to their
/// OpenTelemetry configuration:
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(t => t.AddSource(AITelemetry.SourceName))
///     .WithMetrics(m => m.AddMeter(AITelemetry.SourceName));
/// </code>
/// </para>
/// </remarks>
public sealed class AIOpenTelemetryChatMiddleware : IAIChatMiddleware
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIOpenTelemetryChatMiddleware"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory for OpenTelemetry event logging.</param>
    public AIOpenTelemetryChatMiddleware(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return client.AsBuilder()
            .UseOpenTelemetry(_loggerFactory, sourceName: AITelemetry.SourceName)
            .Use(inner => new SpanTaggingChatClient(inner))
            .Build();
    }

    /// <summary>
    /// Runs inside the gen_ai span the OpenTelemetry client starts, and tags it. A plain pass-through, so a
    /// stream the caller stops reading early is still disposed straight through to the provider.
    /// </summary>
    private sealed class SpanTaggingChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
    {
        public override Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            AITraceTags.Apply(System.Diagnostics.Activity.Current);
            return base.GetResponseAsync(messages, options, cancellationToken);
        }

        public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // Called from inside the OpenTelemetry client's stream, once its span has started.
            AITraceTags.Apply(System.Diagnostics.Activity.Current);
            return base.GetStreamingResponseAsync(messages, options, cancellationToken);
        }
    }
}

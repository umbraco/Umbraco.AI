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
/// It is registered as the innermost middleware so that <c>Activity.Current</c> is
/// available to all outer middleware for enrichment.
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
            .Build();
    }
}

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.Telemetry;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Embedding middleware that adds OpenTelemetry tracing and metrics using M.E.AI's built-in
/// <see cref="OpenTelemetryEmbeddingGenerator{TInput, TEmbedding}"/>. Emits <c>gen_ai.*</c>
/// semantic convention spans and metrics (operation duration, token usage).
/// </summary>
/// <remarks>
/// This middleware has zero overhead when no OpenTelemetry listener is configured.
/// It is registered as the innermost middleware so that <c>Activity.Current</c> is
/// available to all outer middleware for enrichment.
/// </remarks>
public sealed class AIOpenTelemetryEmbeddingMiddleware : IAIEmbeddingMiddleware
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIOpenTelemetryEmbeddingMiddleware"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory for OpenTelemetry event logging.</param>
    public AIOpenTelemetryEmbeddingMiddleware(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        return generator.AsBuilder()
            .UseOpenTelemetry(_loggerFactory, sourceName: AITelemetry.SourceName)
            .Build();
    }
}

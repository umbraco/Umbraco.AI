using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Embeddings;

namespace Umbraco.Ai.Core.Analytics.Middleware;

/// <summary>
/// Middleware that records AI embedding usage to the analytics system.
/// </summary>
internal sealed class AiUsageRecordingEmbeddingMiddleware : IAiEmbeddingMiddleware
{
    private readonly IAiUsageRecordingService _usageRecordingService;
    private readonly IAiUsageRecordFactory _factory;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public AiUsageRecordingEmbeddingMiddleware(
        IAiUsageRecordingService usageRecordingService,
        IAiUsageRecordFactory factory,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILoggerFactory loggerFactory)
    {
        _usageRecordingService = usageRecordingService;
        _factory = factory;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        return new AiUsageRecordingEmbeddingGenerator<string, Embedding<float>>(
            generator,
            _usageRecordingService,
            _factory,
            _options,
            _loggerFactory.CreateLogger<AiUsageRecordingEmbeddingGenerator<string, Embedding<float>>>());
    }
}

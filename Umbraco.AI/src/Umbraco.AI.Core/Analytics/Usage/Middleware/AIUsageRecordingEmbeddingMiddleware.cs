using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Analytics.Usage.Middleware;

/// <summary>
/// Middleware that records AI embedding usage to the analytics system.
/// </summary>
internal sealed class AIUsageRecordingEmbeddingMiddleware : IAIEmbeddingMiddleware
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIUsageRecordingService _usageRecordingService;
    private readonly IAIUsageRecordFactory _factory;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public AIUsageRecordingEmbeddingMiddleware(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIUsageRecordingService usageRecordingService,
        IAIUsageRecordFactory factory,
        IOptionsMonitor<AIAnalyticsOptions> options,
        ILoggerFactory loggerFactory)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _usageRecordingService = usageRecordingService;
        _factory = factory;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        return new AIUsageRecordingEmbeddingGenerator<string, Embedding<float>>(
            generator,
            _runtimeContextAccessor,
            _usageRecordingService,
            _factory,
            _options,
            _loggerFactory.CreateLogger<AIUsageRecordingEmbeddingGenerator<string, Embedding<float>>>());
    }
}

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Embeddings;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Analytics.Middleware;

/// <summary>
/// Middleware that records AI embedding usage to the analytics system.
/// </summary>
internal sealed class AiUsageRecordingEmbeddingMiddleware : IAiEmbeddingMiddleware
{
    private readonly IAiUsageRecordingService _usageRecordingService;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public AiUsageRecordingEmbeddingMiddleware(
        IAiUsageRecordingService usageRecordingService,
        IBackOfficeSecurityAccessor securityAccessor,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILoggerFactory loggerFactory)
    {
        _usageRecordingService = usageRecordingService;
        _securityAccessor = securityAccessor;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        return new AiUsageRecordingEmbeddingGenerator<string, Embedding<float>>(
            generator,
            _usageRecordingService,
            _securityAccessor,
            _options,
            _loggerFactory.CreateLogger<AiUsageRecordingEmbeddingGenerator<string, Embedding<float>>>());
    }
}

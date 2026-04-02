using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.SpeechToText;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.Analytics.Usage.Middleware;

/// <summary>
/// Middleware that records AI speech-to-text usage to the analytics system.
/// </summary>
internal sealed class AIUsageRecordingSpeechToTextMiddleware : IAISpeechToTextMiddleware
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIUsageRecordingService _usageRecordingService;
    private readonly IAIUsageRecordFactory _factory;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly ILogger<AIUsageRecordingSpeechToTextClient> _logger;

    public AIUsageRecordingSpeechToTextMiddleware(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIUsageRecordingService usageRecordingService,
        IAIUsageRecordFactory factory,
        IOptionsMonitor<AIAnalyticsOptions> options,
        ILogger<AIUsageRecordingSpeechToTextClient> logger)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _usageRecordingService = usageRecordingService;
        _factory = factory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public ISpeechToTextClient Apply(ISpeechToTextClient client)
    {
        return new AIUsageRecordingSpeechToTextClient(
            client,
            _runtimeContextAccessor,
            _usageRecordingService,
            _factory,
            _options,
            _logger);
    }
}

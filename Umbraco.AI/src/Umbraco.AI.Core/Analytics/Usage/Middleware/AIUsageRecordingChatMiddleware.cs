using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Analytics.Usage.Middleware;

/// <summary>
/// Middleware that records AI chat usage to the analytics system.
/// </summary>
internal sealed class AIUsageRecordingChatMiddleware : IAIChatMiddleware
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIUsageRecordingService _usageRecordingService;
    private readonly IAIUsageRecordFactory _factory;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly ILogger<AIUsageRecordingChatClient> _logger;

    public AIUsageRecordingChatMiddleware(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIUsageRecordingService usageRecordingService,
        IAIUsageRecordFactory factory,
        IOptionsMonitor<AIAnalyticsOptions> options,
        ILogger<AIUsageRecordingChatClient> logger)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _usageRecordingService = usageRecordingService;
        _factory = factory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new AIUsageRecordingChatClient(
            client,
            _runtimeContextAccessor,
            _usageRecordingService,
            _factory,
            _options,
            _logger);
    }
}

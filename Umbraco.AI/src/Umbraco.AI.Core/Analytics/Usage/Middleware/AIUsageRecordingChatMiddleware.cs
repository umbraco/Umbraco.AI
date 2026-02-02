using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Analytics.Usage.Middleware;

/// <summary>
/// Middleware that records AI chat usage to the analytics system.
/// </summary>
internal sealed class AIUsageRecordingChatMiddleware : IAiChatMiddleware
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiUsageRecordingService _usageRecordingService;
    private readonly IAiUsageRecordFactory _factory;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly ILogger<AIUsageRecordingChatClient> _logger;

    public AIUsageRecordingChatMiddleware(
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiUsageRecordingService usageRecordingService,
        IAiUsageRecordFactory factory,
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

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;

namespace Umbraco.Ai.Core.Analytics.Usage.Middleware;

/// <summary>
/// Middleware that records AI chat usage to the analytics system.
/// </summary>
internal sealed class AiUsageRecordingChatMiddleware : IAiChatMiddleware
{
    private readonly IAiUsageRecordingService _usageRecordingService;
    private readonly IAiUsageRecordFactory _factory;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILogger<AiUsageRecordingChatClient> _logger;

    public AiUsageRecordingChatMiddleware(
        IAiUsageRecordingService usageRecordingService,
        IAiUsageRecordFactory factory,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILogger<AiUsageRecordingChatClient> logger)
    {
        _usageRecordingService = usageRecordingService;
        _factory = factory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new AiUsageRecordingChatClient(
            client,
            _usageRecordingService,
            _factory,
            _options,
            _logger);
    }
}

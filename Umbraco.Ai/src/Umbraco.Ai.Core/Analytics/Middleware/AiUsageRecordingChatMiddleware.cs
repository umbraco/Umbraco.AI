using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Analytics.Middleware;

/// <summary>
/// Middleware that records AI chat usage to the analytics system.
/// </summary>
internal sealed class AiUsageRecordingChatMiddleware : IAiChatMiddleware
{
    private readonly IAiUsageRecordingService _usageRecordingService;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILogger<AiUsageRecordingChatClient> _logger;

    public AiUsageRecordingChatMiddleware(
        IAiUsageRecordingService usageRecordingService,
        IBackOfficeSecurityAccessor securityAccessor,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILogger<AiUsageRecordingChatClient> logger)
    {
        _usageRecordingService = usageRecordingService;
        _securityAccessor = securityAccessor;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new AiUsageRecordingChatClient(
            client,
            _usageRecordingService,
            _securityAccessor,
            _options,
            _logger);
    }
}

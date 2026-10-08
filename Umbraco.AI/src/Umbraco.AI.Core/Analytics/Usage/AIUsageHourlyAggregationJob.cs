using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Hosting;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Background service that periodically aggregates raw usage records into hourly statistics.
/// Runs continuously, processing completed hours and catching up on any missed periods.
/// </summary>
internal sealed class AIUsageHourlyAggregationJob : UmbracoAIRecurringHostedServiceBase
{
    private readonly IAIUsageAggregationService _aggregationService;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IMainDom _mainDom;
    private readonly ILogger<AIUsageHourlyAggregationJob> _logger;

    // Run every 5 minutes
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    public AIUsageHourlyAggregationJob(
        IAIUsageAggregationService aggregationService,
        IOptionsMonitor<AIAnalyticsOptions> options,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        IMainDom mainDom,
        ILogger<AIUsageHourlyAggregationJob> logger)
        : base(logger, CheckInterval, StartupDelay)
    {
        _aggregationService = aggregationService;
        _options = options;
        _runtimeState = runtimeState;
        _serverRoleAccessor = serverRoleAccessor;
        _mainDom = mainDom;
        _logger = logger;
    }

    public override async Task PerformExecuteAsync(object? state)
    {
        // Don't run if analytics is disabled
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("Analytics disabled, skipping hourly aggregation");
            return;
        }

        // Don't run unless Umbraco is running
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        // Don't run on replicas nor unknown role servers
        switch (_serverRoleAccessor.CurrentServerRole)
        {
            case ServerRole.Subscriber:
                _logger.LogDebug("AI Usage Hourly Aggregation will not run on subscriber servers.");
                return;
            case ServerRole.Unknown:
                _logger.LogDebug("AI Usage Hourly Aggregation will not run on servers with unknown role.");
                return;
            case ServerRole.Single:
            case ServerRole.SchedulingPublisher:
            default:
                break;
        }

        // Ensure we do not run if not main domain
        if (!_mainDom.IsMainDom)
        {
            _logger.LogDebug("AI Usage Hourly Aggregation will not run if not MainDom.");
            return;
        }

        // Only process completed hours
        var now = DateTime.UtcNow;
        var lastCompletedHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-1);

        await _aggregationService.AggregatePendingHoursAsync(lastCompletedHour, CancellationToken.None);
    }
}

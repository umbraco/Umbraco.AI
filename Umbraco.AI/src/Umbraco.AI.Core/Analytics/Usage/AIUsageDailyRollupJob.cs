using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Hosting;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Background service that periodically rolls up hourly statistics into daily statistics.
/// Runs daily, processing completed days and catching up on any missed periods.
/// </summary>
internal sealed class AIUsageDailyRollupJob : UmbracoAIRecurringHostedServiceBase
{
    private readonly IAIUsageAggregationService _aggregationService;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IMainDom _mainDom;
    private readonly ILogger<AIUsageDailyRollupJob> _logger;

    // Run every hour (will process if needed)
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    public AIUsageDailyRollupJob(
        IAIUsageAggregationService aggregationService,
        IOptionsMonitor<AIAnalyticsOptions> options,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        IMainDom mainDom,
        ILogger<AIUsageDailyRollupJob> logger)
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
            _logger.LogDebug("Analytics disabled, skipping daily rollup");
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
                _logger.LogDebug("AI Usage Daily Rollup will not run on subscriber servers.");
                return;
            case ServerRole.Unknown:
                _logger.LogDebug("AI Usage Daily Rollup will not run on servers with unknown role.");
                return;
            case ServerRole.Single:
            case ServerRole.SchedulingPublisher:
            default:
                break;
        }

        // Ensure we do not run if not main domain
        if (!_mainDom.IsMainDom)
        {
            _logger.LogDebug("AI Usage Daily Rollup will not run if not MainDom.");
            return;
        }

        // Only process completed days (yesterday and earlier)
        var yesterday = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-1), DateTimeKind.Utc);

        await _aggregationService.RollUpPendingDaysAsync(yesterday, CancellationToken.None);
    }
}

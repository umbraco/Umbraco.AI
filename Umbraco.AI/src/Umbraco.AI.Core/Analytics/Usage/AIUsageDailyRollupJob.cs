using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Background service that periodically rolls up hourly statistics into daily statistics.
/// Runs daily, processing completed days and catching up on any missed periods.
/// </summary>
internal sealed class AIUsageDailyRollupJob : RecurringHostedServiceBase
{
    private readonly IAIUsageAggregationService _aggregationService;
    private readonly IAIUsageStatisticsRepository _statisticsRepository;
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
        IAIUsageStatisticsRepository statisticsRepository,
        IOptionsMonitor<AIAnalyticsOptions> options,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        IMainDom mainDom,
        ILogger<AIUsageDailyRollupJob> logger)
        : base(logger, CheckInterval, StartupDelay)
    {
        _aggregationService = aggregationService;
        _statisticsRepository = statisticsRepository;
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

        await ProcessMissingDaysAsync(CancellationToken.None);
    }

    /// <summary>
    /// Processes all days that need rollup, from last aggregated to yesterday.
    /// </summary>
    private async Task ProcessMissingDaysAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var yesterday = GetDayStart(now.AddDays(-1)); // Only process completed days (yesterday and earlier)

        // Get last aggregated daily period
        var lastAggregatedPeriod = await _statisticsRepository.GetLastAggregatedDailyPeriodAsync(ct);

        DateTime startFromDay;

        if (lastAggregatedPeriod == null)
        {
            // No previous daily aggregation - check if we have any hourly stats
            var lastHourlyPeriod = await _statisticsRepository.GetLastAggregatedHourlyPeriodAsync(ct);

            if (lastHourlyPeriod == null)
            {
                _logger.LogDebug("No hourly statistics found, nothing to roll up into daily");
                return;
            }

            // Start from the day of the first hourly stat
            startFromDay = GetDayStart(lastHourlyPeriod.Value);
            _logger.LogInformation(
                "First daily rollup: starting from {StartDay} (first hourly stat: {FirstHourly})",
                startFromDay,
                lastHourlyPeriod);
        }
        else
        {
            // Start from next day after last aggregated
            startFromDay = lastAggregatedPeriod.Value.AddDays(1);
            _logger.LogDebug(
                "Last aggregated day: {LastDay}, processing from {StartDay}",
                lastAggregatedPeriod,
                startFromDay);
        }

        // Only process if start day is not in the future
        if (startFromDay > yesterday)
        {
            _logger.LogDebug("No completed days to process");
            return;
        }

        // Process all missing days sequentially
        var currentDay = startFromDay;
        var processedCount = 0;

        while (currentDay <= yesterday && !ct.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Rolling up daily statistics for: {Day}", currentDay);
                await _aggregationService.AggregateDailyAsync(currentDay, ct);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to roll up day {Day}, will retry on next run",
                    currentDay);

                // Stop processing and retry from this day on next run
                break;
            }

            currentDay = currentDay.AddDays(1);
        }

        if (processedCount > 0)
        {
            _logger.LogInformation(
                "Processed {Count} days from {Start} to {End}",
                processedCount,
                startFromDay,
                startFromDay.AddDays(processedCount - 1));
        }
    }

    /// <summary>
    /// Gets the start of the day (midnight UTC) for a given timestamp.
    /// </summary>
    private static DateTime GetDayStart(DateTime timestamp)
    {
        return new DateTime(
            timestamp.Year,
            timestamp.Month,
            timestamp.Day,
            0,
            0,
            0,
            DateTimeKind.Utc);
    }
}

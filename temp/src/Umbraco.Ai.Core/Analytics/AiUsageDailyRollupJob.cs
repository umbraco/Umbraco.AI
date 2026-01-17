using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.Ai.Core.Analytics;

/// <summary>
/// Background service that periodically rolls up hourly statistics into daily statistics.
/// Runs daily, processing completed days and catching up on any missed periods.
/// </summary>
internal sealed class AiUsageDailyRollupJob : BackgroundService
{
    private readonly IAiUsageAggregationService _aggregationService;
    private readonly IAiUsageStatisticsRepository _statisticsRepository;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILogger<AiUsageDailyRollupJob> _logger;

    // Run every 6 hours (will process if needed)
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    public AiUsageDailyRollupJob(
        IAiUsageAggregationService aggregationService,
        IAiUsageStatisticsRepository statisticsRepository,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILogger<AiUsageDailyRollupJob> logger)
    {
        _aggregationService = aggregationService;
        _statisticsRepository = statisticsRepository;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Usage Daily Rollup Job started");

        // Wait a bit on startup to let other services initialize
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_options.CurrentValue.Enabled)
                {
                    _logger.LogDebug("Analytics disabled, skipping daily rollup");
                    await Task.Delay(CheckInterval, stoppingToken);
                    continue;
                }

                await ProcessMissingDaysAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in daily rollup job, will retry on next run");
            }

            // Wait before next check
            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("AI Usage Daily Rollup Job stopped");
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

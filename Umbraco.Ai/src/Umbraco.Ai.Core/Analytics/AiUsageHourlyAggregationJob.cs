using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.Ai.Core.Analytics;

/// <summary>
/// Background service that periodically aggregates raw usage records into hourly statistics.
/// Runs continuously, processing completed hours and catching up on any missed periods.
/// </summary>
internal sealed class AiUsageHourlyAggregationJob : BackgroundService
{
    private readonly IAiUsageAggregationService _aggregationService;
    private readonly IAiUsageRecordRepository _recordRepository;
    private readonly IAiUsageStatisticsRepository _statisticsRepository;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILogger<AiUsageHourlyAggregationJob> _logger;

    // Run every 5 minutes
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    public AiUsageHourlyAggregationJob(
        IAiUsageAggregationService aggregationService,
        IAiUsageRecordRepository recordRepository,
        IAiUsageStatisticsRepository statisticsRepository,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILogger<AiUsageHourlyAggregationJob> logger)
    {
        _aggregationService = aggregationService;
        _recordRepository = recordRepository;
        _statisticsRepository = statisticsRepository;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Usage Hourly Aggregation Job started");

        // Wait a bit on startup to let other services initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_options.CurrentValue.Enabled)
                {
                    _logger.LogDebug("Analytics disabled, skipping hourly aggregation");
                    await Task.Delay(CheckInterval, stoppingToken);
                    continue;
                }

                await ProcessMissingHoursAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in hourly aggregation job, will retry on next run");
            }

            // Wait before next check
            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("AI Usage Hourly Aggregation Job stopped");
    }

    /// <summary>
    /// Processes all hours that need aggregation, from last aggregated to current completed hour.
    /// </summary>
    private async Task ProcessMissingHoursAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var currentCompletedHour = GetHourStart(now.AddHours(-1)); // Only process completed hours

        // Get last aggregated period
        var lastAggregatedPeriod = await _statisticsRepository.GetLastAggregatedHourlyPeriodAsync(ct);

        DateTime startFromHour;

        if (lastAggregatedPeriod == null)
        {
            // No previous aggregation - check if we have any raw records
            var firstRecordTimestamp = await _recordRepository.GetLastRecordTimestampAsync(ct);

            if (firstRecordTimestamp == null)
            {
                _logger.LogDebug("No usage records found, nothing to aggregate");
                return;
            }

            // Start from the hour of the first record
            startFromHour = GetHourStart(firstRecordTimestamp.Value);
            _logger.LogInformation(
                "First hourly aggregation: starting from {StartHour} (first record timestamp: {FirstRecord})",
                startFromHour,
                firstRecordTimestamp);
        }
        else
        {
            // Start from next hour after last aggregated
            startFromHour = lastAggregatedPeriod.Value.AddHours(1);
            _logger.LogDebug(
                "Last aggregated hour: {LastHour}, processing from {StartHour}",
                lastAggregatedPeriod,
                startFromHour);
        }

        // Process all missing hours sequentially
        var currentHour = startFromHour;
        var processedCount = 0;

        while (currentHour <= currentCompletedHour && !ct.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Aggregating hour: {Hour}", currentHour);
                await _aggregationService.AggregateHourlyAsync(currentHour, ct);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to aggregate hour {Hour}, will retry on next run",
                    currentHour);

                // Stop processing and retry from this hour on next run
                break;
            }

            currentHour = currentHour.AddHours(1);
        }

        if (processedCount > 0)
        {
            _logger.LogInformation(
                "Processed {Count} hours from {Start} to {End}",
                processedCount,
                startFromHour,
                startFromHour.AddHours(processedCount - 1));
        }
        else if (startFromHour <= currentCompletedHour)
        {
            _logger.LogDebug("No new completed hours to process");
        }
    }

    /// <summary>
    /// Gets the start of the hour for a given timestamp.
    /// </summary>
    private static DateTime GetHourStart(DateTime timestamp)
    {
        return new DateTime(
            timestamp.Year,
            timestamp.Month,
            timestamp.Day,
            timestamp.Hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}

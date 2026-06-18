using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Recurring background job that rolls up hourly statistics into daily statistics.
/// Runs hourly, processing completed days and catching up on any missed periods.
/// </summary>
internal sealed class AIUsageDailyRollupJob : RecurringBackgroundJobBase
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    private readonly IAIUsageAggregationService _aggregationService;
    private readonly IAIUsageStatisticsRepository _statisticsRepository;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly ILogger<AIUsageDailyRollupJob> _logger;

    public AIUsageDailyRollupJob(
        IAIUsageAggregationService aggregationService,
        IAIUsageStatisticsRepository statisticsRepository,
        IOptionsMonitor<AIAnalyticsOptions> options,
        ILogger<AIUsageDailyRollupJob> logger)
        : base(CheckInterval)
    {
        _aggregationService = aggregationService;
        _statisticsRepository = statisticsRepository;
        _options = options;
        _logger = logger;
    }

    public override TimeSpan Delay => StartupDelay;

    public override async Task RunJobAsync(CancellationToken cancellationToken)
    {
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("Analytics disabled, skipping daily rollup");
            return;
        }

        await ProcessMissingDaysAsync(cancellationToken);
    }

    private async Task ProcessMissingDaysAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var yesterday = GetDayStart(now.AddDays(-1)); // Only process completed days (yesterday and earlier)

        var lastAggregatedPeriod = await _statisticsRepository.GetLastAggregatedDailyPeriodAsync(ct);

        DateTime startFromDay;

        if (lastAggregatedPeriod == null)
        {
            var lastHourlyPeriod = await _statisticsRepository.GetLastAggregatedHourlyPeriodAsync(ct);

            if (lastHourlyPeriod == null)
            {
                _logger.LogDebug("No hourly statistics found, nothing to roll up into daily");
                return;
            }

            startFromDay = GetDayStart(lastHourlyPeriod.Value);
            _logger.LogInformation(
                "First daily rollup: starting from {StartDay} (first hourly stat: {FirstHourly})",
                startFromDay,
                lastHourlyPeriod);
        }
        else
        {
            startFromDay = lastAggregatedPeriod.Value.AddDays(1);
            _logger.LogDebug(
                "Last aggregated day: {LastDay}, processing from {StartDay}",
                lastAggregatedPeriod,
                startFromDay);
        }

        if (startFromDay > yesterday)
        {
            _logger.LogDebug("No completed days to process");
            return;
        }

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

    private static DateTime GetDayStart(DateTime timestamp) => new(
        timestamp.Year,
        timestamp.Month,
        timestamp.Day,
        0,
        0,
        0,
        DateTimeKind.Utc);
}

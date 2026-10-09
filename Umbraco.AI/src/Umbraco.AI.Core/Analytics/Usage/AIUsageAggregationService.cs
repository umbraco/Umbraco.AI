using Microsoft.Extensions.Logging;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Service for aggregating raw usage records into hourly and daily statistics.
/// </summary>
internal sealed class AIUsageAggregationService : IAIUsageAggregationService
{
    private readonly IAIUsageRecordRepository _recordRepository;
    private readonly IAIUsageStatisticsRepository _statisticsRepository;
    private readonly ILogger<AIUsageAggregationService> _logger;

    public AIUsageAggregationService(
        IAIUsageRecordRepository recordRepository,
        IAIUsageStatisticsRepository statisticsRepository,
        ILogger<AIUsageAggregationService> logger)
    {
        _recordRepository = recordRepository;
        _statisticsRepository = statisticsRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AggregateHourlyAsync(DateTime hourStart, CancellationToken ct = default)
    {
        // Validate hour boundary
        if (hourStart.Minute != 0 || hourStart.Second != 0 || hourStart.Millisecond != 0)
        {
            throw new ArgumentException(
                "Hour start must be on the hour boundary (minute, second, millisecond must be 0).",
                nameof(hourStart));
        }

        var hourEnd = hourStart.AddHours(1);

        _logger.LogDebug(
            "Starting hourly aggregation for period {HourStart} to {HourEnd}",
            hourStart,
            hourEnd);

        try
        {
            // Get raw records for the hour
            var records = await _recordRepository.GetRecordsByPeriodAsync(
                hourStart,
                hourEnd,
                ct);

            var recordList = records.ToList();

            if (recordList.Count == 0)
            {
                _logger.LogDebug(
                    "No usage records found for hour {HourStart}, skipping aggregation",
                    hourStart);
                return;
            }

            _logger.LogDebug(
                "Aggregating {RecordCount} usage records for hour {HourStart}",
                recordList.Count,
                hourStart);

            var statistics = AIUsageStatisticsGrouping.GroupRecords(recordList, _ => hourStart);

            _logger.LogDebug(
                "Aggregated {RecordCount} records into {StatisticsCount} statistics groups",
                recordList.Count,
                statistics.Count);

            // Idempotent upsert: delete existing stats for this period, then insert new
            await _statisticsRepository.DeleteHourlyForPeriodAsync(hourStart, ct);
            await _statisticsRepository.SaveHourlyBatchAsync(statistics, ct);

            // Delete raw records after successful aggregation
            await _recordRepository.DeleteRecordsByPeriodAsync(hourStart, hourEnd, ct);

            _logger.LogInformation(
                "Completed hourly aggregation for period {HourStart}: {RecordCount} records → {StatisticsCount} statistics groups",
                hourStart,
                recordList.Count,
                statistics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to aggregate usage records for hour {HourStart}",
                hourStart);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task AggregateDailyAsync(DateTime day, CancellationToken ct = default)
    {
        // Validate day boundary
        if (day.Hour != 0 || day.Minute != 0 || day.Second != 0 || day.Millisecond != 0)
        {
            throw new ArgumentException(
                "Day must be at midnight UTC (hour, minute, second, millisecond must be 0).",
                nameof(day));
        }

        var dayEnd = day.AddDays(1);

        _logger.LogDebug(
            "Starting daily aggregation for period {Day} to {DayEnd}",
            day,
            dayEnd);

        try
        {
            // Get hourly stats for the day
            var hourlyStats = await _statisticsRepository.GetHourlyByPeriodAsync(
                day,
                dayEnd,
                filter: null,
                ct);

            var hourlyStatsList = hourlyStats.ToList();

            if (hourlyStatsList.Count == 0)
            {
                _logger.LogDebug(
                    "No hourly statistics found for day {Day}, skipping daily aggregation",
                    day);
                return;
            }

            _logger.LogDebug(
                "Aggregating {HourlyStatsCount} hourly statistics for day {Day}",
                hourlyStatsList.Count,
                day);

            var dailyStatistics = AIUsageStatisticsGrouping.GroupStatistics(hourlyStatsList, _ => day);

            _logger.LogDebug(
                "Aggregated {HourlyStatsCount} hourly statistics into {DailyStatsCount} daily statistics groups",
                hourlyStatsList.Count,
                dailyStatistics.Count);

            // Idempotent upsert: delete existing stats for this period, then insert new
            await _statisticsRepository.DeleteDailyForPeriodAsync(day, ct);
            await _statisticsRepository.SaveDailyBatchAsync(dailyStatistics, ct);

            _logger.LogInformation(
                "Completed daily aggregation for period {Day}: {HourlyStatsCount} hourly stats → {DailyStatsCount} daily stats",
                day,
                hourlyStatsList.Count,
                dailyStatistics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to aggregate hourly statistics for day {Day}",
                day);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task AggregatePendingHoursAsync(DateTime lastCompletedHour, CancellationToken ct = default)
    {
        var lastAggregatedPeriod = await _statisticsRepository.GetLastAggregatedHourlyPeriodAsync(ct);

        DateTime startFromHour;

        if (lastAggregatedPeriod == null)
        {
            var firstRecordTimestamp = await _recordRepository.GetFirstRecordTimestampAsync(ct);

            if (firstRecordTimestamp == null)
            {
                _logger.LogDebug("No usage records found, nothing to aggregate");
                return;
            }

            startFromHour = GetHourStart(firstRecordTimestamp.Value);
            _logger.LogInformation(
                "First hourly aggregation: starting from {StartHour} (first record timestamp: {FirstRecord})",
                startFromHour,
                firstRecordTimestamp);
        }
        else
        {
            await AggregateSkippedHoursAsync(ct);

            startFromHour = lastAggregatedPeriod.Value.AddHours(1);
            _logger.LogDebug(
                "Last aggregated hour: {LastHour}, processing from {StartHour}",
                lastAggregatedPeriod,
                startFromHour);
        }

        var currentHour = startFromHour;
        var processedCount = 0;

        while (currentHour <= lastCompletedHour && !ct.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Aggregating hour: {Hour}", currentHour);
                await AggregateHourlyAsync(currentHour, ct);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to aggregate hour {Hour}, will retry on next run",
                    currentHour);

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
        else if (startFromHour <= lastCompletedHour)
        {
            _logger.LogDebug("No new completed hours to process");
        }
    }

    /// <inheritdoc />
    public async Task RollUpPendingDaysAsync(DateTime lastCompletedDay, CancellationToken ct = default)
    {
        // A day is rolled up from its hourly statistics, so it must wait until the hourly aggregation has
        // reached all its hours, or its daily total would leave them out for good.
        var lastReadyDay = await GetLastDayReadyForRollupAsync(lastCompletedDay, ct);

        var lastAggregatedPeriod = await _statisticsRepository.GetLastAggregatedDailyPeriodAsync(ct);

        DateTime startFromDay;

        var firstHourlyPeriod = await _statisticsRepository.GetFirstAggregatedHourlyPeriodAsync(ct);

        if (lastAggregatedPeriod == null)
        {
            if (firstHourlyPeriod == null)
            {
                _logger.LogDebug("No hourly statistics found, nothing to roll up into daily");
                return;
            }

            startFromDay = GetDayStart(firstHourlyPeriod.Value);
            _logger.LogInformation(
                "First daily rollup: starting from {StartDay} (first hourly stat: {FirstHourly})",
                startFromDay,
                firstHourlyPeriod);
        }
        else
        {
            // Earlier versions started the first rollup from the latest hourly statistic, skipping the days
            // before it. Those days have no daily statistics, so rolling them up can't overwrite anything.
            var firstDailyPeriod = await _statisticsRepository.GetFirstAggregatedDailyPeriodAsync(ct);
            if (firstHourlyPeriod != null && firstDailyPeriod != null)
            {
                await RollUpDaysAsync(GetDayStart(firstHourlyPeriod.Value), Min(firstDailyPeriod.Value.AddDays(-1), lastReadyDay), ct);
            }

            startFromDay = lastAggregatedPeriod.Value.AddDays(1);
            _logger.LogDebug(
                "Last aggregated day: {LastDay}, processing from {StartDay}",
                lastAggregatedPeriod,
                startFromDay);
        }

        if (startFromDay > lastReadyDay)
        {
            _logger.LogDebug("No completed days to process");
            return;
        }

        await RollUpDaysAsync(startFromDay, lastReadyDay, ct);
    }

    /// <inheritdoc />
    public Task DeleteHourlyStatisticsOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
        => _statisticsRepository.DeleteHourlyOlderThanAsync(cutoff, ct);

    /// <inheritdoc />
    public Task DeleteDailyStatisticsOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
        => _statisticsRepository.DeleteDailyOlderThanAsync(cutoff, ct);

    /// <summary>
    /// Aggregates raw records older than the first hourly statistic. Earlier versions started the first run
    /// from the latest record instead of the earliest, which left those records behind for good. Those
    /// hours have no hourly statistics, so aggregating them can't overwrite anything. Each pass deletes the
    /// hour's records, so it walks only hours that hold records and stops once none are left.
    /// </summary>
    private async Task AggregateSkippedHoursAsync(CancellationToken ct)
    {
        var firstAggregatedPeriod = await _statisticsRepository.GetFirstAggregatedHourlyPeriodAsync(ct);
        if (firstAggregatedPeriod == null)
        {
            return;
        }

        DateTime? previousHour = null;

        while (!ct.IsCancellationRequested)
        {
            var firstRecordTimestamp = await _recordRepository.GetFirstRecordTimestampAsync(ct);
            if (firstRecordTimestamp == null)
            {
                return;
            }

            var hour = GetHourStart(firstRecordTimestamp.Value);
            if (hour >= firstAggregatedPeriod.Value || hour == previousHour)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Aggregating skipped hour: {Hour}", hour);
                await AggregateHourlyAsync(hour, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to aggregate skipped hour {Hour}, will retry on next run", hour);
                return;
            }

            previousHour = hour;
        }
    }

    /// <summary>
    /// Gets the last day, up to <paramref name="latestDay"/>, whose hours have all been aggregated. Hourly
    /// aggregation deletes each hour's raw records, so a day that still has raw records has hours it
    /// hasn't reached yet.
    /// </summary>
    private async Task<DateTime> GetLastDayReadyForRollupAsync(DateTime latestDay, CancellationToken ct)
    {
        var firstRecordTimestamp = await _recordRepository.GetFirstRecordTimestampAsync(ct);
        return firstRecordTimestamp is null
            ? latestDay
            : Min(latestDay, GetDayStart(firstRecordTimestamp.Value).AddDays(-1));
    }

    /// <summary>
    /// Rolls up each day from <paramref name="firstDay"/> to <paramref name="lastDay"/> inclusive, stopping
    /// at the first failure so the next run retries from there.
    /// </summary>
    private async Task RollUpDaysAsync(DateTime firstDay, DateTime lastDay, CancellationToken ct)
    {
        var currentDay = firstDay;
        var processedCount = 0;

        while (currentDay <= lastDay && !ct.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Rolling up daily statistics for: {Day}", currentDay);
                await AggregateDailyAsync(currentDay, ct);
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
                firstDay,
                firstDay.AddDays(processedCount - 1));
        }
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    private static DateTime GetHourStart(DateTime timestamp) => new(
        timestamp.Year,
        timestamp.Month,
        timestamp.Day,
        timestamp.Hour,
        0,
        0,
        DateTimeKind.Utc);

    private static DateTime GetDayStart(DateTime timestamp) => new(
        timestamp.Year,
        timestamp.Month,
        timestamp.Day,
        0,
        0,
        0,
        DateTimeKind.Utc);
}

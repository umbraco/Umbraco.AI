using Microsoft.Extensions.Logging;

namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Service for aggregating raw usage records into hourly and daily statistics.
/// </summary>
internal sealed class AiUsageAggregationService : IAiUsageAggregationService
{
    private readonly IAiUsageRecordRepository _recordRepository;
    private readonly IAiUsageStatisticsRepository _statisticsRepository;
    private readonly ILogger<AiUsageAggregationService> _logger;

    public AiUsageAggregationService(
        IAiUsageRecordRepository recordRepository,
        IAiUsageStatisticsRepository statisticsRepository,
        ILogger<AiUsageAggregationService> logger)
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

        _logger.LogInformation(
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
                _logger.LogInformation(
                    "No usage records found for hour {HourStart}, skipping aggregation",
                    hourStart);
                return;
            }

            _logger.LogDebug(
                "Aggregating {RecordCount} usage records for hour {HourStart}",
                recordList.Count,
                hourStart);

            // Group and aggregate by dimensions
            var statistics = recordList
                .GroupBy(r => new
                {
                    r.ProviderId,
                    r.ModelId,
                    r.ProfileId,
                    r.Capability,
                    r.UserId,
                    r.EntityType,
                    r.FeatureType
                })
                .Select(g =>
                {
                    // Find first record with non-null/non-empty ProfileAlias and UserName
                    var recordWithNames = g.FirstOrDefault(r => !string.IsNullOrEmpty(r.ProfileAlias)) ?? g.First();

                    return new AiUsageStatistics
                    {
                        Id = Guid.NewGuid(),
                        Period = hourStart,
                        ProviderId = g.Key.ProviderId,
                        ModelId = g.Key.ModelId,
                        ProfileId = g.Key.ProfileId,
                        ProfileAlias = recordWithNames.ProfileAlias,
                        Capability = g.Key.Capability,
                        UserId = g.Key.UserId,
                        UserName = recordWithNames.UserName,
                        EntityType = g.Key.EntityType,
                        FeatureType = g.Key.FeatureType,
                        RequestCount = g.Count(),
                        SuccessCount = g.Count(r => r.Status == "Succeeded"),
                        FailureCount = g.Count(r => r.Status == "Failed"),
                        InputTokens = g.Sum(r => r.InputTokens),
                        OutputTokens = g.Sum(r => r.OutputTokens),
                        TotalTokens = g.Sum(r => r.TotalTokens),
                        TotalDurationMs = g.Sum(r => r.DurationMs),
                        CreatedAt = DateTime.UtcNow
                    };
                })
                .ToList();

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

        _logger.LogInformation(
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
                _logger.LogInformation(
                    "No hourly statistics found for day {Day}, skipping daily aggregation",
                    day);
                return;
            }

            _logger.LogDebug(
                "Aggregating {HourlyStatsCount} hourly statistics for day {Day}",
                hourlyStatsList.Count,
                day);

            // Group and aggregate by dimensions
            var dailyStatistics = hourlyStatsList
                .GroupBy(s => new
                {
                    s.ProviderId,
                    s.ModelId,
                    s.ProfileId,
                    s.Capability,
                    s.UserId,
                    s.EntityType,
                    s.FeatureType
                })
                .Select(g =>
                {
                    // Find first record with non-null/non-empty ProfileAlias and UserName
                    var statsWithNames = g.FirstOrDefault(s => !string.IsNullOrEmpty(s.ProfileAlias)) ?? g.First();

                    return new AiUsageStatistics
                    {
                        Id = Guid.NewGuid(),
                        Period = day,
                        ProviderId = g.Key.ProviderId,
                        ModelId = g.Key.ModelId,
                        ProfileId = g.Key.ProfileId,
                        ProfileAlias = statsWithNames.ProfileAlias,
                        Capability = g.Key.Capability,
                        UserId = g.Key.UserId,
                        UserName = statsWithNames.UserName,
                        EntityType = g.Key.EntityType,
                        FeatureType = g.Key.FeatureType,
                        RequestCount = g.Sum(s => s.RequestCount),
                        SuccessCount = g.Sum(s => s.SuccessCount),
                        FailureCount = g.Sum(s => s.FailureCount),
                        InputTokens = g.Sum(s => s.InputTokens),
                        OutputTokens = g.Sum(s => s.OutputTokens),
                        TotalTokens = g.Sum(s => s.TotalTokens),
                        TotalDurationMs = g.Sum(s => s.TotalDurationMs),
                        CreatedAt = DateTime.UtcNow
                    };
                })
                .ToList();

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
}

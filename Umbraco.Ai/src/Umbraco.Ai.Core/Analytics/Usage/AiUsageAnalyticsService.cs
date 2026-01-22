using Microsoft.Extensions.Logging;

namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Service for querying aggregated AI usage statistics with hybrid live + historical data.
/// </summary>
internal sealed class AiUsageAnalyticsService : IAiUsageAnalyticsService
{
    private readonly IAiUsageRecordRepository _recordRepository;
    private readonly IAiUsageStatisticsRepository _statisticsRepository;
    private readonly ILogger<AiUsageAnalyticsService> _logger;

    public AiUsageAnalyticsService(
        IAiUsageRecordRepository recordRepository,
        IAiUsageStatisticsRepository statisticsRepository,
        ILogger<AiUsageAnalyticsService> logger)
    {
        _recordRepository = recordRepository;
        _statisticsRepository = statisticsRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiUsageSummary> GetSummaryAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        AiUsageFilter? filter = null,
        CancellationToken ct = default)
    {
        var granularity = DetermineGranularity(from, to, requestedGranularity);
        var statistics = await GetStatisticsAsync(from, to, granularity, filter, ct);

        var statsList = statistics.ToList();

        if (statsList.Count == 0)
        {
            return new AiUsageSummary
            {
                TotalRequests = 0,
                InputTokens = 0,
                OutputTokens = 0,
                TotalTokens = 0,
                SuccessCount = 0,
                FailureCount = 0,
                SuccessRate = 0,
                AverageDurationMs = 0
            };
        }

        var totalRequests = statsList.Sum(s => s.RequestCount);
        var successCount = statsList.Sum(s => s.SuccessCount);
        var failureCount = statsList.Sum(s => s.FailureCount);
        var totalDurationMs = statsList.Sum(s => s.TotalDurationMs);

        return new AiUsageSummary
        {
            TotalRequests = totalRequests,
            InputTokens = statsList.Sum(s => s.InputTokens),
            OutputTokens = statsList.Sum(s => s.OutputTokens),
            TotalTokens = statsList.Sum(s => s.TotalTokens),
            SuccessCount = successCount,
            FailureCount = failureCount,
            SuccessRate = totalRequests > 0 ? (double)successCount / totalRequests : 0,
            AverageDurationMs = totalRequests > 0 ? (int)(totalDurationMs / totalRequests) : 0
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageTimeSeriesPoint>> GetTimeSeriesAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        AiUsageFilter? filter = null,
        CancellationToken ct = default)
    {
        var granularity = DetermineGranularity(from, to, requestedGranularity);
        var statistics = await GetStatisticsAsync(from, to, granularity, filter, ct);

        var timeSeries = statistics
            .GroupBy(s => s.Period)
            .Select(g => new AiUsageTimeSeriesPoint
            {
                Timestamp = g.Key,
                RequestCount = g.Sum(s => s.RequestCount),
                TotalTokens = g.Sum(s => s.TotalTokens),
                InputTokens = g.Sum(s => s.InputTokens),
                OutputTokens = g.Sum(s => s.OutputTokens),
                SuccessCount = g.Sum(s => s.SuccessCount),
                FailureCount = g.Sum(s => s.FailureCount)
            })
            .OrderBy(p => p.Timestamp)
            .ToList();

        return timeSeries;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageBreakdownItem>> GetBreakdownByProviderAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        CancellationToken ct = default)
    {
        var granularity = DetermineGranularity(from, to, requestedGranularity);
        var statistics = await GetStatisticsAsync(from, to, granularity, filter: null, ct);

        return CalculateBreakdown(
            statistics,
            s => s.ProviderId,
            null, // Providers don't have friendly names
            "Unknown Provider");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageBreakdownItem>> GetBreakdownByModelAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        CancellationToken ct = default)
    {
        var granularity = DetermineGranularity(from, to, requestedGranularity);
        var statistics = await GetStatisticsAsync(from, to, granularity, filter: null, ct);

        return CalculateBreakdown(
            statistics,
            s => s.ModelId,
            null, // Models don't have friendly names
            "Unknown Model");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageBreakdownItem>> GetBreakdownByProfileAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        CancellationToken ct = default)
    {
        var granularity = DetermineGranularity(from, to, requestedGranularity);
        var statistics = await GetStatisticsAsync(from, to, granularity, filter: null, ct);

        return CalculateBreakdown(
            statistics,
            s => s.ProfileId.ToString(),
            s => s.ProfileAlias, // Include profile alias as friendly name
            "Unknown Profile");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiUsageBreakdownItem>> GetBreakdownByUserAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requestedGranularity = null,
        CancellationToken ct = default)
    {
        var granularity = DetermineGranularity(from, to, requestedGranularity);
        var statistics = await GetStatisticsAsync(from, to, granularity, filter: null, ct);

        return CalculateBreakdown(
            statistics,
            s => s.UserId ?? "Anonymous",
            s => s.UserName, // Include user name as friendly name
            "Anonymous");
    }

    /// <summary>
    /// Determines the appropriate granularity based on date range.
    /// </summary>
    private AiUsagePeriod DetermineGranularity(
        DateTime from,
        DateTime to,
        AiUsagePeriod? requested)
    {
        if (requested.HasValue)
            return requested.Value;

        var daySpan = (to - from).TotalDays;

        // Use hourly for up to 7 days, daily for longer periods
        return daySpan <= 7 ? AiUsagePeriod.Hourly : AiUsagePeriod.Daily;
    }

    /// <summary>
    /// Gets statistics with hybrid querying: aggregated stats + live current hour data.
    /// </summary>
    private async Task<IEnumerable<AiUsageStatistics>> GetStatisticsAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod granularity,
        AiUsageFilter? filter,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var currentPeriodStart = granularity == AiUsagePeriod.Hourly
            ? GetHourStart(now)
            : GetDayStart(now);

        // Query aggregated statistics (everything before current period)
        var aggregatedStats = await GetAggregatedStatisticsAsync(
            from,
            to < currentPeriodStart ? to : currentPeriodStart,
            granularity,
            filter,
            ct);

        var allStats = aggregatedStats.ToList();

        // If query range includes current period, add live data from raw records
        if (to > currentPeriodStart)
        {
            try
            {
                var liveStats = await GetLiveStatisticsAsync(
                    from > currentPeriodStart ? from : currentPeriodStart,
                    to < now ? to : now,
                    currentPeriodStart,
                    filter,
                    ct);

                if (liveStats != null)
                {
                    allStats.AddRange(liveStats);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get live statistics, using aggregated data only");
            }
        }

        return allStats;
    }

    /// <summary>
    /// Gets aggregated statistics from hourly or daily tables.
    /// </summary>
    private async Task<IEnumerable<AiUsageStatistics>> GetAggregatedStatisticsAsync(
        DateTime from,
        DateTime to,
        AiUsagePeriod granularity,
        AiUsageFilter? filter,
        CancellationToken ct)
    {
        if (from >= to)
            return [];

        if (granularity == AiUsagePeriod.Hourly)
        {
            return await _statisticsRepository.GetHourlyByPeriodAsync(from, to, filter, ct);
        }
        else
        {
            return await _statisticsRepository.GetDailyByPeriodAsync(from, to, filter, ct);
        }
    }

    /// <summary>
    /// Gets live statistics from raw usage records for the current hour/day.
    /// Aggregates in-memory to match statistics format.
    /// </summary>
    private async Task<IEnumerable<AiUsageStatistics>?> GetLiveStatisticsAsync(
        DateTime from,
        DateTime to,
        DateTime currentPeriodStart,
        AiUsageFilter? filter,
        CancellationToken ct)
    {
        var records = await _recordRepository.GetRecordsByPeriodAsync(from, to, ct);
        var recordList = records.ToList();

        if (recordList.Count == 0)
            return null;

        // Apply filter if specified
        if (filter != null)
        {
            recordList = ApplyFilterToRecords(recordList, filter);
        }

        if (recordList.Count == 0)
            return null;

        // Aggregate in-memory, grouped by dimensions
        var aggregated = recordList
            .GroupBy(r => new
            {
                r.ProviderId,
                r.ModelId,
                r.ProfileId,
                r.ProfileAlias,
                r.Capability,
                r.UserId,
                r.UserName,
                r.EntityType,
                r.FeatureType
            })
            .Select(g => new AiUsageStatistics
            {
                Id = Guid.NewGuid(),
                Period = currentPeriodStart,
                ProviderId = g.Key.ProviderId,
                ModelId = g.Key.ModelId,
                ProfileId = g.Key.ProfileId,
                ProfileAlias = g.Key.ProfileAlias,
                Capability = g.Key.Capability,
                UserId = g.Key.UserId,
                UserName = g.Key.UserName,
                EntityType = g.Key.EntityType,
                FeatureType = g.Key.FeatureType,
                RequestCount = g.Count(),
                SuccessCount = g.Count(r => r.Status == "Succeeded"),
                FailureCount = g.Count(r => r.Status == "Failed"),
                InputTokens = g.Sum(r => (long)r.InputTokens),
                OutputTokens = g.Sum(r => (long)r.OutputTokens),
                TotalTokens = g.Sum(r => (long)r.TotalTokens),
                TotalDurationMs = g.Sum(r => r.DurationMs),
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        _logger.LogDebug(
            "Aggregated {RecordCount} live records into {StatsCount} statistics groups",
            recordList.Count,
            aggregated.Count);

        return aggregated;
    }

    /// <summary>
    /// Applies filter to raw usage records.
    /// </summary>
    private static List<AiUsageRecord> ApplyFilterToRecords(
        List<AiUsageRecord> records,
        AiUsageFilter filter)
    {
        var filtered = records.AsEnumerable();

        if (filter.ProviderId != null)
            filtered = filtered.Where(r => r.ProviderId == filter.ProviderId);

        if (filter.ModelId != null)
            filtered = filtered.Where(r => r.ModelId == filter.ModelId);

        if (filter.ProfileId != null)
            filtered = filtered.Where(r => r.ProfileId == filter.ProfileId.Value);

        if (filter.Capability != null)
            filtered = filtered.Where(r => r.Capability == filter.Capability.Value);

        if (filter.UserId != null)
            filtered = filtered.Where(r => r.UserId == filter.UserId);

        if (filter.EntityType != null)
            filtered = filtered.Where(r => r.EntityType == filter.EntityType);

        if (filter.FeatureType != null)
            filtered = filtered.Where(r => r.FeatureType == filter.FeatureType);

        return filtered.ToList();
    }

    /// <summary>
    /// Calculates breakdown by a specific dimension with percentages.
    /// </summary>
    private static IEnumerable<AiUsageBreakdownItem> CalculateBreakdown(
        IEnumerable<AiUsageStatistics> statistics,
        Func<AiUsageStatistics, string> dimensionSelector,
        Func<AiUsageStatistics, string?>? nameSelector,
        string unknownLabel)
    {
        var statsList = statistics.ToList();

        if (statsList.Count == 0)
            return [];

        var totalRequests = statsList.Sum(s => s.RequestCount);

        var breakdown = statsList
            .GroupBy(s => new
            {
                Dimension = dimensionSelector(s),
                Name = nameSelector?.Invoke(s)
            })
            .Select(g => new AiUsageBreakdownItem
            {
                Dimension = string.IsNullOrEmpty(g.Key.Dimension) ? unknownLabel : g.Key.Dimension,
                DimensionName = g.Key.Name,
                RequestCount = g.Sum(s => s.RequestCount),
                TotalTokens = g.Sum(s => s.TotalTokens),
                Percentage = totalRequests > 0
                    ? (double)g.Sum(s => s.RequestCount) / totalRequests * 100
                    : 0
            })
            .OrderByDescending(b => b.RequestCount)
            .ToList();

        return breakdown;
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

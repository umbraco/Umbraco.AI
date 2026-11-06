namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Service for querying aggregated AI usage statistics with hybrid live + historical data.
/// </summary>
internal sealed class AIUsageAnalyticsService : IAIUsageAnalyticsService
{
    private readonly IAIUsageStatisticsReader _reader;

    public AIUsageAnalyticsService(IAIUsageStatisticsReader reader)
    {
        _reader = reader;
    }

    private Task<IEnumerable<AIUsageStatistics>> GetStatisticsAsync(
        DateTime from,
        DateTime to,
        AIUsagePeriod granularity,
        AIUsageFilter? filter,
        CancellationToken ct)
        => _reader.GetStatisticsAsync(from, to, granularity, filter, ct);

    /// <inheritdoc />
    public async Task<AIUsageSummary> GetSummaryAsync(
        DateTime from,
        DateTime to,
        AIUsagePeriod? requestedGranularity = null,
        AIUsageFilter? filter = null,
        CancellationToken ct = default)
    {
        var granularity = DetermineGranularity(from, to, requestedGranularity);
        var statistics = await GetStatisticsAsync(from, to, granularity, filter, ct);

        var statsList = statistics.ToList();

        if (statsList.Count == 0)
        {
            return new AIUsageSummary
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

        return new AIUsageSummary
        {
            TotalRequests = totalRequests,
            NestedRequestCount = statsList.Sum(s => s.NestedRequestCount),
            InputTokens = statsList.Sum(s => s.InputTokens),
            CachedInputTokens = AIUsageTokenAggregation.SumOrNull(statsList, s => s.CachedInputTokens),
            OutputTokens = statsList.Sum(s => s.OutputTokens),
            TotalTokens = statsList.Sum(s => s.TotalTokens),
            SuccessCount = successCount,
            FailureCount = failureCount,
            SuccessRate = totalRequests > 0 ? (double)successCount / totalRequests : 0,
            AverageDurationMs = totalRequests > 0 ? (int)(totalDurationMs / totalRequests) : 0
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIUsageTimeSeriesPoint>> GetTimeSeriesAsync(
        DateTime from,
        DateTime to,
        AIUsagePeriod? requestedGranularity = null,
        AIUsageFilter? filter = null,
        CancellationToken ct = default)
    {
        var granularity = DetermineGranularity(from, to, requestedGranularity);
        var statistics = await GetStatisticsAsync(from, to, granularity, filter, ct);

        var timeSeries = statistics
            .GroupBy(s => s.Period)
            .Select(g => new AIUsageTimeSeriesPoint
            {
                Timestamp = g.Key,
                RequestCount = g.Sum(s => s.RequestCount),
                TotalTokens = g.Sum(s => s.TotalTokens),
                InputTokens = g.Sum(s => s.InputTokens),
                CachedInputTokens = AIUsageTokenAggregation.SumOrNull(g, s => s.CachedInputTokens),
                OutputTokens = g.Sum(s => s.OutputTokens),
                SuccessCount = g.Sum(s => s.SuccessCount),
                FailureCount = g.Sum(s => s.FailureCount)
            })
            .OrderBy(p => p.Timestamp)
            .ToList();

        return timeSeries;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIUsageBreakdownItem>> GetBreakdownByProviderAsync(
        DateTime from,
        DateTime to,
        AIUsagePeriod? requestedGranularity = null,
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
    public async Task<IEnumerable<AIUsageBreakdownItem>> GetBreakdownByModelAsync(
        DateTime from,
        DateTime to,
        AIUsagePeriod? requestedGranularity = null,
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
    public async Task<IEnumerable<AIUsageBreakdownItem>> GetBreakdownByProfileAsync(
        DateTime from,
        DateTime to,
        AIUsagePeriod? requestedGranularity = null,
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
    public async Task<IEnumerable<AIUsageBreakdownItem>> GetBreakdownByUserAsync(
        DateTime from,
        DateTime to,
        AIUsagePeriod? requestedGranularity = null,
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
    private AIUsagePeriod DetermineGranularity(
        DateTime from,
        DateTime to,
        AIUsagePeriod? requested)
    {
        if (requested.HasValue)
            return requested.Value;

        var daySpan = (to - from).TotalDays;

        // Use hourly for up to 7 days, daily for longer periods
        return daySpan <= 7 ? AIUsagePeriod.Hourly : AIUsagePeriod.Daily;
    }

    /// <summary>
    /// Calculates breakdown by a specific dimension with percentages.
    /// </summary>
    private static IEnumerable<AIUsageBreakdownItem> CalculateBreakdown(
        IEnumerable<AIUsageStatistics> statistics,
        Func<AIUsageStatistics, string> dimensionSelector,
        Func<AIUsageStatistics, string?>? nameSelector,
        string unknownLabel)
    {
        var statsList = statistics.ToList();

        if (statsList.Count == 0)
            return [];

        var totalRequests = statsList.Sum(s => s.RequestCount);

        // Group by the dimension only: names can change over the range, so each row takes the latest one.
        var breakdown = statsList
            .OrderBy(s => s.Period)
            .GroupBy(dimensionSelector)
            .Select(g => new AIUsageBreakdownItem
            {
                Dimension = string.IsNullOrEmpty(g.Key) ? unknownLabel : g.Key,
                DimensionName = nameSelector is null ? null : AIUsageStatisticsGrouping.Latest(g, nameSelector),
                RequestCount = g.Sum(s => s.RequestCount),
                TotalTokens = g.Sum(s => s.TotalTokens),
                CachedInputTokens = AIUsageTokenAggregation.SumOrNull(g, s => s.CachedInputTokens),
                Percentage = totalRequests > 0
                    ? (double)g.Sum(s => s.RequestCount) / totalRequests * 100
                    : 0
            })
            .OrderByDescending(b => b.RequestCount)
            .ToList();

        return breakdown;
    }
}

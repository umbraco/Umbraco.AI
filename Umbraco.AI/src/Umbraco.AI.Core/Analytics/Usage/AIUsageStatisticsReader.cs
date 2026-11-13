using Microsoft.Extensions.Logging;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Reads usage statistics across every layer that holds part of a range: daily rows, hourly rows and raw
/// records not yet aggregated.
/// </summary>
internal interface IAIUsageStatisticsReader
{
    /// <summary>
    /// Gets statistics for a range, all in one granularity (hourly or daily rows).
    /// </summary>
    Task<IEnumerable<AIUsageStatistics>> GetStatisticsAsync(
        DateTime from,
        DateTime to,
        AIUsagePeriod granularity,
        AIUsageFilter? filter,
        CancellationToken ct);

    /// <summary>
    /// Gets where the aggregation jobs have got to: the one place the daily/hourly split line is worked out.
    /// </summary>
    Task<AIUsageStatisticsCoverage> GetCoverageAsync(CancellationToken ct);

    /// <summary>
    /// Gets statistics for a range summed per hour (or per key of a dimension) at the finest resolution still
    /// held: hourly rows and raw records wherever they exist, and daily rows only for the older days whose
    /// hourly rows have been cleaned up. The summing happens in the database.
    /// </summary>
    /// <remarks>
    /// <paramref name="from"/> and <paramref name="to"/> are the instants a range of local days starts and
    /// ends at. An hour belongs to the range when it starts inside it, and a UTC day when its midday does,
    /// so whole hours and whole days are returned and none is split or counted in two ranges.
    /// </remarks>
    /// <param name="from">Start instant (inclusive).</param>
    /// <param name="to">End instant (exclusive).</param>
    /// <param name="dimension">The dimension to group by, or null to group by hour (day for daily rows).</param>
    /// <param name="hourlyOnly">
    /// Serve the whole range from hourly rows and raw records, and return no daily rows. For hour-by-hour
    /// views, where the oldest day that still has hourly rows must not be left to its daily row.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task<AIUsageLayeredTotals> GetTotalsAsync(
        DateTime from,
        DateTime to,
        AIUsageBreakdownDimension? dimension,
        bool hourlyOnly,
        CancellationToken ct);
}

/// <summary>
/// Summed statistics for a range, split by the resolution each row was stored at.
/// </summary>
/// <param name="Hourly">Rows one hour wide (<see cref="AIUsageStatisticsTotal.Period"/> is the hour start, UTC) when grouped by period.</param>
/// <param name="Daily">Rows one UTC day wide (<see cref="AIUsageStatisticsTotal.Period"/> is midnight UTC) when grouped by period.</param>
internal sealed record AIUsageLayeredTotals(
    IReadOnlyList<AIUsageStatisticsTotal> Hourly,
    IReadOnlyList<AIUsageStatisticsTotal> Daily);

/// <summary>
/// Default <see cref="IAIUsageStatisticsReader"/>. Splits a range where the aggregation jobs have actually
/// got to rather than at the clock, so nothing is missed while a job is behind and nothing is counted twice.
/// </summary>
internal sealed class AIUsageStatisticsReader : IAIUsageStatisticsReader
{
    private readonly IAIUsageRecordRepository _recordRepository;
    private readonly IAIUsageStatisticsRepository _statisticsRepository;
    private readonly ILogger<AIUsageStatisticsReader> _logger;

    public AIUsageStatisticsReader(
        IAIUsageRecordRepository recordRepository,
        IAIUsageStatisticsRepository statisticsRepository,
        ILogger<AIUsageStatisticsReader> logger)
    {
        _recordRepository = recordRepository;
        _statisticsRepository = statisticsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets statistics from every layer that holds part of the range: daily rows up to the last rolled-up
    /// day, hourly rows up to the last aggregated hour, then raw records. Each layer starts where the
    /// previous one ends.
    /// </summary>
    public async Task<IEnumerable<AIUsageStatistics>> GetStatisticsAsync(
        DateTime from,
        DateTime to,
        AIUsagePeriod granularity,
        AIUsageFilter? filter,
        CancellationToken ct)
    {
        var periodStart = granularity == AIUsagePeriod.Hourly
            ? (Func<DateTime, DateTime>)GetHourStart
            : GetDayStart;

        // Raw records are deleted once their hour is aggregated, so they hold everything after the last
        // aggregated hour. With no hourly statistics yet, everything is still raw.
        var lastHourlyPeriod = await _statisticsRepository.GetLastAggregatedHourlyPeriodAsync(ct);
        var rawFrom = lastHourlyPeriod?.AddHours(1) ?? from;

        var allStats = new List<AIUsageStatistics>();

        if (granularity == AIUsagePeriod.Hourly)
        {
            allStats.AddRange(await GetHourlyStatisticsAsync(from, Earliest(to, rawFrom), filter, ct));
        }
        else
        {
            // Daily rows only exist for days the rollup job has completed; later days are still hourly.
            var lastDailyPeriod = await _statisticsRepository.GetLastAggregatedDailyPeriodAsync(ct);
            var hourlyFrom = lastDailyPeriod?.AddDays(1) ?? from;

            allStats.AddRange(await GetDailyStatisticsAsync(from, Earliest(to, hourlyFrom), filter, ct));

            var hourlyStats = await GetHourlyStatisticsAsync(
                Latest(from, hourlyFrom), Earliest(to, rawFrom), filter, ct);
            allStats.AddRange(AIUsageStatisticsGrouping.GroupStatistics(hourlyStats, periodStart));
        }

        var liveStats = await GetLiveStatisticsAsync(
            Latest(from, rawFrom),
            Earliest(to, DateTime.UtcNow),
            periodStart,
            filter,
            ct);

        if (liveStats != null)
        {
            allStats.AddRange(liveStats);
        }

        return allStats;
    }

    /// <inheritdoc />
    public async Task<AIUsageStatisticsCoverage> GetCoverageAsync(CancellationToken ct)
        => new()
        {
            FirstHourlyPeriod = await _statisticsRepository.GetFirstAggregatedHourlyPeriodAsync(ct),
            LastHourlyPeriod = await _statisticsRepository.GetLastAggregatedHourlyPeriodAsync(ct),
            LastDailyPeriod = await _statisticsRepository.GetLastAggregatedDailyPeriodAsync(ct),
        };

    /// <inheritdoc />
    public async Task<AIUsageLayeredTotals> GetTotalsAsync(
        DateTime from,
        DateTime to,
        AIUsageBreakdownDimension? dimension,
        bool hourlyOnly,
        CancellationToken ct)
    {
        var coverage = await GetCoverageAsync(ct);
        var lastHourlyPeriod = coverage.LastHourlyPeriod;

        // Daily rows serve only the days before the oldest retained hour (the day it falls in is rolled
        // up already, so is whole in its daily row) and only the days the rollup job has completed.
        // Hourly rows and raw records serve everything from there on.
        // When only hours are wanted there is no daily layer: every hour still held is served from its hourly row.
        var dailyBoundary = (hourlyOnly ? null : coverage.DailyBoundary) ?? DateTime.MinValue;

        // A UTC day belongs to the range when its midday does.
        var daily = await GetDailyTotalsAsync(
            from.AddHours(-12), Earliest(to.AddHours(-12), dailyBoundary), dimension, ct);

        // An hour belongs to the range when it starts inside it, so a zone on a half-hour offset takes the
        // first whole hour after its midnight and the hour that starts before its next one.
        var hourFrom = Latest(CeilingToHour(from), dailyBoundary);
        var hourTo = CeilingToHour(to);

        // Raw records are deleted once their hour is aggregated, so they hold everything after the last
        // aggregated hour. With no hourly statistics yet, everything not in a daily row is still raw.
        var rawFrom = lastHourlyPeriod?.AddHours(1) ?? hourFrom;

        var hourly = (await GetHourlyTotalsAsync(hourFrom, Earliest(hourTo, rawFrom), dimension, ct)).ToList();

        var rawTotals = await GetLiveTotalsAsync(
            Latest(hourFrom, rawFrom), Earliest(hourTo, DateTime.UtcNow), dimension, ct);
        hourly.AddRange(rawTotals);

        return new AIUsageLayeredTotals(hourly, daily);
    }

    private async Task<IReadOnlyList<AIUsageStatisticsTotal>> GetHourlyTotalsAsync(
        DateTime from,
        DateTime to,
        AIUsageBreakdownDimension? dimension,
        CancellationToken ct)
        => from < to ? await _statisticsRepository.GetHourlyTotalsAsync(from, to, dimension, ct) : [];

    private async Task<IReadOnlyList<AIUsageStatisticsTotal>> GetDailyTotalsAsync(
        DateTime from,
        DateTime to,
        AIUsageBreakdownDimension? dimension,
        CancellationToken ct)
        => from < to ? await _statisticsRepository.GetDailyTotalsAsync(from, to, dimension, ct) : [];

    /// <summary>
    /// Sums the raw usage records that are not aggregated yet. A failure is logged and the aggregated data
    /// is used alone.
    /// </summary>
    private async Task<IReadOnlyList<AIUsageStatisticsTotal>> GetLiveTotalsAsync(
        DateTime from,
        DateTime to,
        AIUsageBreakdownDimension? dimension,
        CancellationToken ct)
    {
        if (from >= to)
            return [];

        try
        {
            var records = await _recordRepository.GetRecordsByPeriodAsync(from, to, ct);
            return AIUsageStatisticsTotal.FromRecords(records, dimension);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get live statistics, using aggregated data only");
            return [];
        }
    }

    private async Task<IEnumerable<AIUsageStatistics>> GetHourlyStatisticsAsync(
        DateTime from,
        DateTime to,
        AIUsageFilter? filter,
        CancellationToken ct)
        => from < to ? await _statisticsRepository.GetHourlyByPeriodAsync(from, to, filter, ct) : [];

    private async Task<IEnumerable<AIUsageStatistics>> GetDailyStatisticsAsync(
        DateTime from,
        DateTime to,
        AIUsageFilter? filter,
        CancellationToken ct)
        => from < to ? await _statisticsRepository.GetDailyByPeriodAsync(from, to, filter, ct) : [];

    private static DateTime Earliest(DateTime a, DateTime b) => a < b ? a : b;

    private static DateTime Latest(DateTime a, DateTime b) => a > b ? a : b;

    /// <summary>
    /// Gets live statistics from raw usage records that have not been aggregated yet, bucketed into the
    /// requested granularity. Aggregates in-memory to match statistics format. A failure here is logged and
    /// the aggregated data is used alone.
    /// </summary>
    private async Task<IEnumerable<AIUsageStatistics>?> GetLiveStatisticsAsync(
        DateTime from,
        DateTime to,
        Func<DateTime, DateTime> periodStart,
        AIUsageFilter? filter,
        CancellationToken ct)
    {
        if (from >= to)
            return null;

        try
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

            var aggregated = AIUsageStatisticsGrouping.GroupRecords(recordList, periodStart);

            _logger.LogDebug(
                "Aggregated {RecordCount} live records into {StatsCount} statistics groups",
                recordList.Count,
                aggregated.Count);

            return aggregated;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get live statistics, using aggregated data only");
            return null;
        }
    }

    /// <summary>
    /// Applies filter to raw usage records.
    /// </summary>
    private static List<AIUsageRecord> ApplyFilterToRecords(
        List<AIUsageRecord> records,
        AIUsageFilter filter)
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

    private static DateTime GetHourStart(DateTime timestamp)
        => new(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, DateTimeKind.Utc);

    private static DateTime GetDayStart(DateTime timestamp)
        => new(timestamp.Year, timestamp.Month, timestamp.Day, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime CeilingToHour(DateTime timestamp)
    {
        var hour = GetHourStart(timestamp);
        return hour < timestamp ? hour.AddHours(1) : hour;
    }
}

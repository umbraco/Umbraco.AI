namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Default <see cref="IAIUsageOverviewService"/>, built on the usage statistics layers (daily rows, hourly
/// rows and raw records), summed in the database.
/// </summary>
/// <remarks>
/// Rows are stored per UTC hour or UTC day, so a local day is the sum of the hours that start in it. An
/// hour goes to the local day it starts on, which for a zone on a half-hour offset puts its two straddling
/// hours on the day each starts. Days older than the hourly retention only exist as UTC days; each goes to
/// the local day its midday falls on, the nearest a whole day can come.
/// </remarks>
internal sealed class AIUsageOverviewService : IAIUsageOverviewService
{
    private const string UnknownKey = "unknown";
    private const string AnonymousKey = "anonymous";

    private readonly IAIUsageStatisticsReader _reader;

    public AIUsageOverviewService(IAIUsageStatisticsReader reader)
    {
        _reader = reader;
    }

    /// <inheritdoc />
    public async Task<AIUsageOverview> GetOverviewAsync(AIUsageOverviewQuery query, CancellationToken ct = default)
    {
        ValidateRange(query.From, query.To, query.CompareFrom, query.CompareTo);

        var current = await GetPeriodAsync(query.From, query.To, query, ct);
        var previous = query.CompareFrom is { } compareFrom && query.CompareTo is { } compareTo
            ? await GetPeriodAsync(compareFrom, compareTo, query, ct)
            : null;

        return new AIUsageOverview { Current = current, Previous = previous };
    }

    /// <inheritdoc />
    public async Task<AIUsageBreakdownPage> GetBreakdownPageAsync(AIUsageBreakdownQuery query, CancellationToken ct = default)
    {
        ValidateRange(query.From, query.To, query.CompareFrom, query.CompareTo);

        var current = await LoadGroupsAsync(query.From, query.To, query.TimeZone, query.Dimension, ct);
        var hasCompare = query.CompareFrom is not null && query.CompareTo is not null;
        var previous = hasCompare
            ? await LoadGroupsAsync(query.CompareFrom!.Value, query.CompareTo!.Value, query.TimeZone, query.Dimension, ct)
            : [];

        var previousByKey = previous.ToDictionary(g => g.Key, g => ToCounts(g.Totals));

        var rows = current
            .Select(g => new AIUsageBreakdownRow
            {
                Key = g.Key,
                Name = g.Name,
                Current = ToCounts(g.Totals),
                Previous = hasCompare ? previousByKey.GetValueOrDefault(g.Key) ?? ToCounts([]) : null,
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows
                .Where(r => r.Key.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (r.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        // The grouped rows are few (one per provider, model, profile or user), so sorting and paging them
        // here costs nothing next to reading them.
        var page = Sort(rows, query.Sort, query.Descending)
            .Skip(Math.Max(0, query.Skip))
            .Take(Math.Max(0, query.Take))
            .ToList();

        return new AIUsageBreakdownPage
        {
            Total = rows.Count,
            Items = page,
            Totals = ToCounts(current.SelectMany(g => g.Totals)),
            PreviousTotals = hasCompare ? ToCounts(previous.SelectMany(g => g.Totals)) : null,
        };
    }

    private async Task<AIUsageOverviewPeriod> GetPeriodAsync(
        DateOnly from,
        DateOnly to,
        AIUsageOverviewQuery query,
        CancellationToken ct)
    {
        if (query.GroupBy == AIUsageGroupBy.Hour)
        {
            return await GetHourlyPeriodAsync(from, to, query.TimeZone, ct);
        }

        var fromUtc = LocalDayStartUtc(from, query.TimeZone);
        var toUtc = LocalDayStartUtc(to.AddDays(1), query.TimeZone);
        var layered = await _reader.GetTotalsAsync(fromUtc, toUtc, dimension: null, ct);

        var rows = layered.Hourly
            .Select(t => (Date: LocalDate(t.Period, query.TimeZone), Total: t))
            .Concat(layered.Daily.Select(t => (Date: LocalDate(t.Period.AddHours(12), query.TimeZone), Total: t)))
            .ToList();

        var bucketStart = BucketStart(query.GroupBy, query.WeekStart);

        // Every bucket of the range, so days without usage are zeros rather than gaps.
        var starts = new List<DateOnly>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var start = bucketStart(day);
            if (starts.Count == 0 || starts[^1] != start)
            {
                starts.Add(start);
            }
        }

        var rowsByBucket = rows.ToLookup(r => bucketStart(r.Date), r => r.Total);

        var buckets = starts
            .Select(start => ToBucket(start, rowsByBucket[start]))
            .ToList();

        return new AIUsageOverviewPeriod
        {
            From = from,
            To = to,
            Totals = ToCounts(rows.Select(r => r.Total)),
            Buckets = buckets,
        };
    }

    /// <summary>
    /// One bucket per UTC hour that starts inside the range, so a day that gains or loses an hour on a clock
    /// change has 25 or 23, and a zone on a half-hour offset starts on the first whole hour after its
    /// midnight. Only hourly rows (and raw records) can fill them: days that only survive as daily rows count
    /// as zeros.
    /// </summary>
    private async Task<AIUsageOverviewPeriod> GetHourlyPeriodAsync(
        DateOnly from,
        DateOnly to,
        TimeZoneInfo timeZone,
        CancellationToken ct)
    {
        var fromUtc = LocalDayStartUtc(from, timeZone);
        var toUtc = LocalDayStartUtc(to.AddDays(1), timeZone);

        var layered = await _reader.GetTotalsAsync(fromUtc, toUtc, dimension: null, ct);
        var totalsByHour = layered.Hourly.ToLookup(t => DateTime.SpecifyKind(t.Period, DateTimeKind.Utc));

        var firstHour = CeilingToHour(fromUtc);
        var buckets = new List<AIUsageHourlyBucket>();
        for (var hour = firstHour; hour < toUtc; hour = hour.AddHours(1))
        {
            var counts = ToCounts(totalsByHour[hour]);
            buckets.Add(new AIUsageHourlyBucket
            {
                Start = new DateTimeOffset(hour).ToOffset(timeZone.GetUtcOffset(hour)),
                Requests = counts.Requests,
                NestedRequests = counts.NestedRequests,
                Succeeded = counts.Succeeded,
                Failed = counts.Failed,
                InputTokens = counts.InputTokens,
                CachedInputTokens = counts.CachedInputTokens,
                OutputTokens = counts.OutputTokens,
                TotalTokens = counts.TotalTokens,
                DurationMs = counts.DurationMs,
            });
        }

        return new AIUsageOverviewPeriod
        {
            From = from,
            To = to,
            Totals = ToCounts(totalsByHour.SelectMany(g => g)),
            Buckets = [],
            HourlyBuckets = buckets,
        };
    }

    /// <summary>
    /// The totals of every key of a dimension in a range, named by the latest name seen for the key.
    /// </summary>
    private async Task<List<KeyGroup>> LoadGroupsAsync(
        DateOnly from,
        DateOnly to,
        TimeZoneInfo timeZone,
        AIUsageBreakdownDimension dimension,
        CancellationToken ct)
    {
        var fromUtc = LocalDayStartUtc(from, timeZone);
        var toUtc = LocalDayStartUtc(to.AddDays(1), timeZone);

        var layered = await _reader.GetTotalsAsync(fromUtc, toUtc, dimension, ct);

        return layered.Hourly
            .Concat(layered.Daily)
            .GroupBy(t => KeyOf(t, dimension))
            .Select(g => new KeyGroup(
                g.Key,
                dimension is AIUsageBreakdownDimension.Profile or AIUsageBreakdownDimension.User
                    ? g.OrderBy(t => t.Period).Select(t => t.Name).LastOrDefault(n => !string.IsNullOrEmpty(n))
                    : null,
                g.ToList()))
            .ToList();
    }

    private static string KeyOf(AIUsageStatisticsTotal total, AIUsageBreakdownDimension dimension) => dimension switch
    {
        AIUsageBreakdownDimension.Profile => total.Key is null || total.Key == Guid.Empty.ToString() ? UnknownKey : total.Key,
        AIUsageBreakdownDimension.User => string.IsNullOrEmpty(total.Key) ? AnonymousKey : total.Key,
        _ => string.IsNullOrEmpty(total.Key) ? UnknownKey : total.Key,
    };

    private sealed record KeyGroup(string Key, string? Name, List<AIUsageStatisticsTotal> Totals);

    private static void ValidateRange(DateOnly from, DateOnly to, DateOnly? compareFrom, DateOnly? compareTo)
    {
        if (to < from)
        {
            throw new ArgumentException("The range must end on or after its start.", nameof(to));
        }

        if (compareFrom.HasValue != compareTo.HasValue || (compareFrom.HasValue && compareTo!.Value < compareFrom.Value))
        {
            throw new ArgumentException("The comparison range needs a start and an end, ending on or after its start.", nameof(compareTo));
        }
    }

    /// <summary>
    /// The UTC instant a local day starts at. A day whose midnight doesn't exist (a clock change at midnight)
    /// starts at the first time that does, and one whose midnight happens twice starts at the first of them.
    /// </summary>
    internal static DateTime LocalDayStartUtc(DateOnly day, TimeZoneInfo timeZone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        while (timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(30);
        }

        if (timeZone.IsAmbiguousTime(local))
        {
            // The larger offset is the earlier instant.
            var offset = timeZone.GetAmbiguousTimeOffsets(local).Max();
            return DateTime.SpecifyKind(local - offset, DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), DateTimeKind.Utc);
    }

    private static DateTime CeilingToHour(DateTime utc)
    {
        var hour = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc);
        return hour < utc ? hour.AddHours(1) : hour;
    }

    private static DateOnly LocalDate(DateTime utc, TimeZoneInfo timeZone)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone));

    private static Func<DateOnly, DateOnly> BucketStart(AIUsageGroupBy groupBy, DayOfWeek weekStart) => groupBy switch
    {
        AIUsageGroupBy.Week => day => day.AddDays(-((7 + (int)day.DayOfWeek - (int)weekStart) % 7)),
        AIUsageGroupBy.Month => day => new DateOnly(day.Year, day.Month, 1),
        _ => day => day,
    };

    private static AIUsageBucket ToBucket(DateOnly start, IEnumerable<AIUsageStatisticsTotal> rows)
    {
        var counts = ToCounts(rows);
        return new AIUsageBucket
        {
            Start = start,
            Requests = counts.Requests,
            NestedRequests = counts.NestedRequests,
            Succeeded = counts.Succeeded,
            Failed = counts.Failed,
            InputTokens = counts.InputTokens,
            CachedInputTokens = counts.CachedInputTokens,
            OutputTokens = counts.OutputTokens,
            TotalTokens = counts.TotalTokens,
            DurationMs = counts.DurationMs,
        };
    }

    private static AIUsageCounts ToCounts(IEnumerable<AIUsageStatisticsTotal> rows)
    {
        var list = rows as IReadOnlyCollection<AIUsageStatisticsTotal> ?? rows.ToList();
        return new AIUsageCounts
        {
            Requests = list.Sum(s => s.RequestCount),
            NestedRequests = list.Sum(s => s.NestedRequestCount),
            Succeeded = list.Sum(s => s.SuccessCount),
            Failed = list.Sum(s => s.FailureCount),
            InputTokens = list.Sum(s => s.InputTokens),
            CachedInputTokens = AIUsageTokenAggregation.SumOrNull(list, s => s.CachedInputTokens),
            OutputTokens = list.Sum(s => s.OutputTokens),
            TotalTokens = list.Sum(s => s.TotalTokens),
            DurationMs = list.Sum(s => s.TotalDurationMs),
        };
    }

    private static IEnumerable<AIUsageBreakdownRow> Sort(
        List<AIUsageBreakdownRow> rows,
        AIUsageBreakdownSort sort,
        bool descending)
    {
        Func<AIUsageBreakdownRow, double> number = sort switch
        {
            AIUsageBreakdownSort.TotalTokens => r => r.Current.TotalTokens,
            AIUsageBreakdownSort.SuccessRate => r => r.Current.Requests > 0 ? (double)r.Current.Succeeded / r.Current.Requests : 0,
            AIUsageBreakdownSort.AverageDuration => r => r.Current.Requests > 0 ? (double)r.Current.DurationMs / r.Current.Requests : 0,
            _ => r => r.Current.Requests,
        };

        var byName = StringComparer.OrdinalIgnoreCase;
        string Label(AIUsageBreakdownRow r) => r.Name ?? r.Key;

        if (sort == AIUsageBreakdownSort.Name)
        {
            return descending
                ? rows.OrderByDescending(Label, byName).ThenBy(r => r.Key, StringComparer.Ordinal)
                : rows.OrderBy(Label, byName).ThenBy(r => r.Key, StringComparer.Ordinal);
        }

        // Ties fall back to the name so a page never shuffles between requests.
        return (descending ? rows.OrderByDescending(number) : rows.OrderBy(number))
            .ThenBy(Label, byName)
            .ThenBy(r => r.Key, StringComparer.Ordinal);
    }
}

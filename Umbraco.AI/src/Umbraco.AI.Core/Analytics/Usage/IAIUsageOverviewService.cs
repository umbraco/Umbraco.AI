namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Reads usage for the analytics dashboard: raw counts per local-time bucket and per dimension, for a
/// range and an optional comparison range.
/// </summary>
/// <remarks>
/// Everything is raw counts, so a client works out rates and averages from sums and a bucket, a range or a
/// row always uses the same maths. Ranges are whole local days in the query's time zone.
/// </remarks>
public interface IAIUsageOverviewService
{
    /// <summary>
    /// Gets the totals and the zero-filled time series for a range and, when given, a comparison range.
    /// </summary>
    /// <param name="query">The ranges, time zone and bucket size.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AIUsageOverview> GetOverviewAsync(AIUsageOverviewQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets one page of usage rows broken down by a dimension, with the same rows' counts for the
    /// comparison range when given.
    /// </summary>
    /// <param name="query">The ranges, time zone, dimension, sort, search and page.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AIUsageBreakdownPage> GetBreakdownPageAsync(AIUsageBreakdownQuery query, CancellationToken ct = default);
}

/// <summary>
/// The size of a time series bucket.
/// </summary>
public enum AIUsageGroupBy
{
    /// <summary>
    /// One bucket per hour, reported in <see cref="AIUsageOverviewPeriod.HourlyBuckets"/>. Hours older than
    /// the hourly statistics retention (30 days by default) only survive as daily rows and read as zeros.
    /// The Management API takes hourly ranges of up to 2 local days.
    /// </summary>
    Hour,

    /// <summary>One bucket per local day.</summary>
    Day,

    /// <summary>One bucket per local week.</summary>
    Week,

    /// <summary>One bucket per local calendar month.</summary>
    Month
}

/// <summary>
/// What usage rows are broken down by.
/// </summary>
public enum AIUsageBreakdownDimension
{
    /// <summary>By provider.</summary>
    Provider,

    /// <summary>By model.</summary>
    Model,

    /// <summary>By profile.</summary>
    Profile,

    /// <summary>By user.</summary>
    User
}

/// <summary>
/// What breakdown rows are sorted by.
/// </summary>
public enum AIUsageBreakdownSort
{
    /// <summary>By the row's name (its key when it has none).</summary>
    Name,

    /// <summary>By number of requests.</summary>
    Requests,

    /// <summary>By total tokens.</summary>
    TotalTokens,

    /// <summary>By the share of requests that succeeded.</summary>
    SuccessRate,

    /// <summary>By average request duration.</summary>
    AverageDuration
}

/// <summary>
/// The ranges and shape of a usage overview.
/// </summary>
public class AIUsageOverviewQuery
{
    /// <summary>Gets the first local day of the range.</summary>
    public required DateOnly From { get; init; }

    /// <summary>Gets the last local day of the range (inclusive).</summary>
    public required DateOnly To { get; init; }

    /// <summary>Gets the first local day of the comparison range, if any.</summary>
    public DateOnly? CompareFrom { get; init; }

    /// <summary>Gets the last local day of the comparison range (inclusive), if any.</summary>
    public DateOnly? CompareTo { get; init; }

    /// <summary>Gets the time zone the days are local to. Defaults to UTC.</summary>
    public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;

    /// <summary>
    /// Gets the size of the time series buckets. Hourly buckets cover the whole local days of the range and
    /// come from hourly statistics, so they only exist for as long as those are kept.
    /// </summary>
    public AIUsageGroupBy GroupBy { get; init; } = AIUsageGroupBy.Day;

    /// <summary>Gets the day weeks start on when grouping by week.</summary>
    public DayOfWeek WeekStart { get; init; } = DayOfWeek.Monday;
}

/// <summary>
/// The ranges and shape of a usage breakdown page.
/// </summary>
public class AIUsageBreakdownQuery
{
    /// <summary>Gets the first local day of the range.</summary>
    public required DateOnly From { get; init; }

    /// <summary>Gets the last local day of the range (inclusive).</summary>
    public required DateOnly To { get; init; }

    /// <summary>Gets the first local day of the comparison range, if any.</summary>
    public DateOnly? CompareFrom { get; init; }

    /// <summary>Gets the last local day of the comparison range (inclusive), if any.</summary>
    public DateOnly? CompareTo { get; init; }

    /// <summary>Gets the time zone the days are local to. Defaults to UTC.</summary>
    public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;

    /// <summary>Gets what rows are broken down by.</summary>
    public required AIUsageBreakdownDimension Dimension { get; init; }

    /// <summary>Gets what rows are sorted by.</summary>
    public AIUsageBreakdownSort Sort { get; init; } = AIUsageBreakdownSort.Requests;

    /// <summary>Gets whether the sort runs highest (or last) first.</summary>
    public bool Descending { get; init; } = true;

    /// <summary>Gets text to find in a row's key or name, ignoring case.</summary>
    public string? Search { get; init; }

    /// <summary>Gets how many rows to skip.</summary>
    public int Skip { get; init; }

    /// <summary>Gets how many rows to return.</summary>
    public int Take { get; init; } = 10;
}

/// <summary>
/// Raw usage counts for a bucket, a range or a breakdown row.
/// </summary>
public class AIUsageCounts
{
    /// <summary>Gets the number of requests.</summary>
    public required int Requests { get; init; }

    /// <summary>
    /// Gets how many of <see cref="Requests"/> were made while another tracked AI operation was running.
    /// </summary>
    public int NestedRequests { get; init; }

    /// <summary>Gets the number of successful requests.</summary>
    public required int Succeeded { get; init; }

    /// <summary>Gets the number of failed or blocked requests.</summary>
    public required int Failed { get; init; }

    /// <summary>Gets the input tokens consumed.</summary>
    public required long InputTokens { get; init; }

    /// <summary>
    /// Gets the portion of <see cref="InputTokens"/> served from a provider's prompt cache, or <c>null</c>
    /// when no provider reported it.
    /// </summary>
    public long? CachedInputTokens { get; init; }

    /// <summary>Gets the output tokens generated.</summary>
    public required long OutputTokens { get; init; }

    /// <summary>Gets the total tokens (input + output).</summary>
    public required long TotalTokens { get; init; }

    /// <summary>Gets the time spent in requests, in milliseconds.</summary>
    public required long DurationMs { get; init; }
}

/// <summary>
/// The counts for one time series bucket.
/// </summary>
public sealed class AIUsageBucket : AIUsageCounts
{
    /// <summary>
    /// Gets the local day the bucket starts on: the day itself, the first day of its week or the first of
    /// its month. For the first week or month it can be before the range's own start.
    /// </summary>
    public required DateOnly Start { get; init; }
}

/// <summary>
/// The counts for one hour.
/// </summary>
public sealed class AIUsageHourlyBucket : AIUsageCounts
{
    /// <summary>
    /// Gets the instant the hour starts, written in the time zone's offset at that instant. A day that
    /// gains or loses an hour on a clock change has 25 or 23 of these.
    /// </summary>
    public required DateTimeOffset Start { get; init; }
}

/// <summary>
/// The usage of one range: totals and a bucket for every day, week or month of it.
/// </summary>
public sealed class AIUsageOverviewPeriod
{
    /// <summary>Gets the first local day of the range.</summary>
    public required DateOnly From { get; init; }

    /// <summary>Gets the last local day of the range (inclusive).</summary>
    public required DateOnly To { get; init; }

    /// <summary>Gets the counts for the whole range.</summary>
    public required AIUsageCounts Totals { get; init; }

    /// <summary>
    /// Gets every day, week or month bucket of the range in order, with buckets without usage as zeros.
    /// Empty when grouped by hour.
    /// </summary>
    public required IReadOnlyList<AIUsageBucket> Buckets { get; init; }

    /// <summary>
    /// Gets every hour of the range in order, with hours without usage as zeros. Empty unless grouped by hour.
    /// </summary>
    public IReadOnlyList<AIUsageHourlyBucket> HourlyBuckets { get; init; } = [];
}

/// <summary>
/// The usage overview for a range and its comparison range.
/// </summary>
public sealed class AIUsageOverview
{
    /// <summary>Gets the requested range.</summary>
    public required AIUsageOverviewPeriod Current { get; init; }

    /// <summary>Gets the comparison range, when one was requested.</summary>
    public AIUsageOverviewPeriod? Previous { get; init; }
}

/// <summary>
/// One row of a usage breakdown.
/// </summary>
public sealed class AIUsageBreakdownRow
{
    /// <summary>
    /// Gets the row's identity: a provider ID, model ID, profile ID or user ID, or <c>unknown</c> (provider,
    /// model, profile) or <c>anonymous</c> (user) when usage has none recorded.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>Gets the latest friendly name (profile alias, user name), if the dimension has one.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the row's counts for the range.</summary>
    public required AIUsageCounts Current { get; init; }

    /// <summary>Gets the row's counts for the comparison range (zeros when it had no usage), if one was requested.</summary>
    public AIUsageCounts? Previous { get; init; }
}

/// <summary>
/// One page of usage breakdown rows.
/// </summary>
public sealed class AIUsageBreakdownPage
{
    /// <summary>Gets how many rows match the search, before paging.</summary>
    public required int Total { get; init; }

    /// <summary>Gets the rows of the page.</summary>
    public required IReadOnlyList<AIUsageBreakdownRow> Items { get; init; }

    /// <summary>Gets the counts for the whole range, whatever the search and page.</summary>
    public required AIUsageCounts Totals { get; init; }

    /// <summary>Gets the counts for the whole comparison range, if one was requested.</summary>
    public AIUsageCounts? PreviousTotals { get; init; }
}

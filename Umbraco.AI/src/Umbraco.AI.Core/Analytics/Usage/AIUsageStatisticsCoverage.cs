namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Where the aggregation jobs have got to: the first and last hour that has an hourly row, and the last day
/// that has a daily row. Together they say which resolution Umbraco.AI holds usage at for any given day.
/// </summary>
/// <remarks>
/// <para>
/// Usage is held at three resolutions: raw records for the hours not yet aggregated, hourly rows for recent
/// days, and daily rows for older days once the hourly rows are cleaned up. A day that has a daily row is
/// whole in it; a day from <see cref="DailyBoundary"/> on is only available as hourly rows (and raw records).
/// </para>
/// <para>
/// A consumer that buckets usage into days in a time zone other than UTC reads the days before
/// <see cref="DailyBoundary"/> from daily statistics, and everything from the boundary on from hourly
/// statistics, which can be regrouped into local days. This is the same line the Usage tab splits at.
/// </para>
/// <para>
/// All values are UTC: whatever kind a store hands back, they are returned with
/// <see cref="DateTimeKind.Utc"/>.
/// </para>
/// </remarks>
public sealed class AIUsageStatisticsCoverage
{
    private readonly DateTime? _firstHourlyPeriod;
    private readonly DateTime? _lastHourlyPeriod;
    private readonly DateTime? _lastDailyPeriod;

    /// <summary>
    /// Gets the start of the oldest hour that has an hourly row, or null when there are none.
    /// </summary>
    public DateTime? FirstHourlyPeriod
    {
        get => _firstHourlyPeriod;
        init => _firstHourlyPeriod = AsUtc(value);
    }

    /// <summary>
    /// Gets the start of the most recent hour that has an hourly row, or null when there are none.
    /// Later hours are still raw records.
    /// </summary>
    public DateTime? LastHourlyPeriod
    {
        get => _lastHourlyPeriod;
        init => _lastHourlyPeriod = AsUtc(value);
    }

    /// <summary>
    /// Gets midnight of the most recent day that has a daily row, or null when there are none.
    /// </summary>
    public DateTime? LastDailyPeriod
    {
        get => _lastDailyPeriod;
        init => _lastDailyPeriod = AsUtc(value);
    }

    /// <summary>
    /// Gets the instant (UTC midnight) where daily rows stop being used, or null when there are no daily rows.
    /// </summary>
    /// <remarks>
    /// A null value means there is no daily layer: read every day from hourly statistics.
    /// Otherwise, days before it are read from daily rows; from it on, from hourly rows and raw records.
    /// It is the earlier of two lines: the day after <see cref="LastDailyPeriod"/>, so days the rollup job has
    /// not reached yet are still read hourly; and the first whole day that has hourly rows (the day
    /// <see cref="FirstHourlyPeriod"/> falls in, rounded up), because hourly rows are the finer data and a
    /// part day left at the start by the cleanup job is whole in its daily row. The Usage tab splits at the
    /// same line.
    /// </remarks>
    public DateTime? DailyBoundary
    {
        get
        {
            if (LastDailyPeriod is not { } lastDaily)
            {
                return null;
            }

            var boundary = lastDaily.AddDays(1);
            if (FirstHourlyPeriod is { } firstHourly)
            {
                var firstHourlyDay = CeilingToDay(firstHourly);
                if (firstHourlyDay < boundary)
                {
                    boundary = firstHourlyDay;
                }
            }

            return boundary;
        }
    }

    private static DateTime? AsUtc(DateTime? value)
        => value is { } v ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : null;

    private static DateTime CeilingToDay(DateTime timestamp)
    {
        var day = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, 0, 0, 0, DateTimeKind.Utc);
        return day < timestamp ? day.AddDays(1) : day;
    }
}

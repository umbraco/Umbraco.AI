namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Service for aggregating raw usage records into statistics and cleaning them up.
/// Internal - only called by background aggregation jobs, which own the timing; this service owns the
/// usage record and statistics repositories.
/// </summary>
internal interface IAIUsageAggregationService
{
    /// <summary>
    /// Aggregates raw usage records for a specific hour into hourly statistics.
    /// After successful aggregation, deletes the raw records.
    /// </summary>
    /// <param name="hourStart">The start of the hour to aggregate (must be on the hour boundary).</param>
    /// <param name="ct">Cancellation token.</param>
    Task AggregateHourlyAsync(DateTime hourStart, CancellationToken ct = default);

    /// <summary>
    /// Aggregates hourly statistics for a specific day into daily statistics.
    /// </summary>
    /// <param name="day">The day to aggregate (must be at midnight UTC).</param>
    /// <param name="ct">Cancellation token.</param>
    Task AggregateDailyAsync(DateTime day, CancellationToken ct = default);

    /// <summary>
    /// Aggregates every hour not yet in hourly statistics, up to <paramref name="lastCompletedHour"/>.
    /// Starts after the last aggregated hour (or from the oldest record on the first run), picks up raw
    /// records an earlier first run skipped, and stops at the first failure so the next run retries.
    /// </summary>
    /// <param name="lastCompletedHour">The latest hour that has ended (on the hour boundary).</param>
    /// <param name="ct">Cancellation token.</param>
    Task AggregatePendingHoursAsync(DateTime lastCompletedHour, CancellationToken ct = default);

    /// <summary>
    /// Rolls up every day not yet in daily statistics, up to <paramref name="lastCompletedDay"/>, once all
    /// its hours are in hourly statistics. Picks up days an earlier first run skipped, and stops at the
    /// first failure so the next run retries.
    /// </summary>
    /// <param name="lastCompletedDay">The latest day that has ended (midnight UTC).</param>
    /// <param name="ct">Cancellation token.</param>
    Task RollUpPendingDaysAsync(DateTime lastCompletedDay, CancellationToken ct = default);

    /// <summary>
    /// Deletes hourly statistics older than <paramref name="cutoff"/>.
    /// </summary>
    /// <param name="cutoff">Statistics for periods before this are deleted.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteHourlyStatisticsOlderThanAsync(DateTime cutoff, CancellationToken ct = default);

    /// <summary>
    /// Deletes daily statistics older than <paramref name="cutoff"/>.
    /// </summary>
    /// <param name="cutoff">Statistics for periods before this are deleted.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteDailyStatisticsOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}

namespace Umbraco.Ai.Core.Analytics;

/// <summary>
/// Service for aggregating raw usage records into statistics.
/// Internal - only called by background aggregation jobs.
/// </summary>
internal interface IAiUsageAggregationService
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
}

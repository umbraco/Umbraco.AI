namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Repository for managing aggregated AI usage statistics.
/// Internal - only accessed by aggregation and analytics services.
/// </summary>
internal interface IAIUsageStatisticsRepository
{
    /// <summary>
    /// Gets hourly statistics within a time period, optionally filtered.
    /// </summary>
    /// <param name="from">Start time (inclusive).</param>
    /// <param name="to">End time (exclusive).</param>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of hourly statistics.</returns>
    Task<IEnumerable<AIUsageStatistics>> GetHourlyByPeriodAsync(
        DateTime from,
        DateTime to,
        AIUsageFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets daily statistics within a time period, optionally filtered.
    /// </summary>
    /// <param name="from">Start time (inclusive).</param>
    /// <param name="to">End time (exclusive).</param>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of daily statistics.</returns>
    Task<IEnumerable<AIUsageStatistics>> GetDailyByPeriodAsync(
        DateTime from,
        DateTime to,
        AIUsageFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Saves a batch of hourly statistics.
    /// </summary>
    /// <param name="statistics">The statistics to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveHourlyBatchAsync(
        IEnumerable<AIUsageStatistics> statistics,
        CancellationToken ct = default);

    /// <summary>
    /// Saves a batch of daily statistics.
    /// </summary>
    /// <param name="statistics">The statistics to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveDailyBatchAsync(
        IEnumerable<AIUsageStatistics> statistics,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent period that has been aggregated (hourly).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The most recent aggregated period, or null if no stats exist.</returns>
    Task<DateTime?> GetLastAggregatedHourlyPeriodAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent period that has been aggregated (daily).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The most recent aggregated period, or null if no stats exist.</returns>
    Task<DateTime?> GetLastAggregatedDailyPeriodAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes hourly statistics for a specific period.
    /// Used for idempotent upserts during aggregation.
    /// </summary>
    /// <param name="period">The period to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteHourlyForPeriodAsync(DateTime period, CancellationToken ct = default);

    /// <summary>
    /// Deletes daily statistics for a specific period.
    /// Used for idempotent upserts during aggregation.
    /// </summary>
    /// <param name="period">The period to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteDailyForPeriodAsync(DateTime period, CancellationToken ct = default);

    /// <summary>
    /// Deletes hourly statistics older than the specified date.
    /// Used for retention cleanup.
    /// </summary>
    /// <param name="olderThan">Delete stats with periods older than this date.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteHourlyOlderThanAsync(DateTime olderThan, CancellationToken ct = default);

    /// <summary>
    /// Deletes daily statistics older than the specified date.
    /// Used for retention cleanup.
    /// </summary>
    /// <param name="olderThan">Delete stats with periods older than this date.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteDailyOlderThanAsync(DateTime olderThan, CancellationToken ct = default);
}

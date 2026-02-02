namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Repository for managing raw AI usage records.
/// Internal - only accessed by usage recording and aggregation services.
/// </summary>
internal interface IAiUsageRecordRepository
{
    /// <summary>
    /// Saves a new usage record.
    /// </summary>
    /// <param name="record">The usage record to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveAsync(AiUsageRecord record, CancellationToken ct = default);

    /// <summary>
    /// Gets all usage records within a specific time period.
    /// Used for hourly aggregation.
    /// </summary>
    /// <param name="from">Start time (inclusive).</param>
    /// <param name="to">End time (exclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of usage records in the period.</returns>
    Task<IEnumerable<AiUsageRecord>> GetRecordsByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes all usage records within a specific time period.
    /// Called after successful aggregation to free up storage.
    /// </summary>
    /// <param name="from">Start time (inclusive).</param>
    /// <param name="to">End time (exclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteRecordsByPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the timestamp of the most recent usage record.
    /// Used for monitoring and health checks.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The most recent record timestamp, or null if no records exist.</returns>
    Task<DateTime?> GetLastRecordTimestampAsync(CancellationToken ct = default);
}

namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Service for recording raw AI usage data.
/// Internal - only called by usage recording middleware.
/// </summary>
internal interface IAiUsageRecordingService
{
    /// <summary>
    /// Records a raw usage entry for an AI operation (synchronous persistence).
    /// </summary>
    /// <param name="record">The usage record to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordUsageAsync(AiUsageRecord record, CancellationToken ct = default);

    /// <summary>
    /// Queues recording a usage entry in the background.
    /// This is a fire-and-forget operation that uses the background task queue.
    /// </summary>
    /// <param name="record">The usage record to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the work item is queued (not when it completes).</returns>
    ValueTask QueueRecordUsageAsync(AiUsageRecord record, CancellationToken ct = default);
}

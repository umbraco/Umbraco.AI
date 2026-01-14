namespace Umbraco.Ai.Core.Analytics;

/// <summary>
/// Service for recording raw AI usage data.
/// Internal - only called by usage recording middleware.
/// </summary>
internal interface IAiUsageRecordingService
{
    /// <summary>
    /// Records a raw usage entry for an AI operation.
    /// </summary>
    /// <param name="record">The usage record to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordUsageAsync(AiUsageRecord record, CancellationToken ct = default);
}

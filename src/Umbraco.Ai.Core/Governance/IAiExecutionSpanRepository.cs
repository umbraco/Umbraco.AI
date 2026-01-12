namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Internal repository interface for AI execution span persistence operations.
/// </summary>
internal interface IAiExecutionSpanRepository
{
    /// <summary>
    /// Retrieves all execution spans for a specific trace.
    /// </summary>
    /// <param name="traceId">The trace ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of execution spans ordered by sequence number.</returns>
    Task<IEnumerable<AiExecutionSpan>> GetByTraceIdAsync(Guid traceId, CancellationToken ct);

    /// <summary>
    /// Saves a single execution span to the database.
    /// </summary>
    /// <param name="span">The span to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved span with updated ID.</returns>
    Task<AiExecutionSpan> SaveAsync(AiExecutionSpan span, CancellationToken ct);

    /// <summary>
    /// Saves multiple execution spans in a single batch operation.
    /// </summary>
    /// <param name="spans">The spans to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveBatchAsync(IEnumerable<AiExecutionSpan> spans, CancellationToken ct);
}

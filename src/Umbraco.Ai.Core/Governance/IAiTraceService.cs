using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Service interface for AI governance tracing operations.
/// Provides methods for recording, retrieving, and managing AI execution traces.
/// </summary>
public interface IAiTraceService
{
    /// <summary>
    /// Starts a new AI trace record for the current operation.
    /// </summary>
    /// <param name="capability">The AI capability being used.</param>
    /// <param name="additionalProperties">Additional context properties from the request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created trace record.</returns>
    Task<AiTrace> StartTraceAsync(
        AiCapability capability,
        IDictionary<string, object?>? additionalProperties,
        CancellationToken ct);

    /// <summary>
    /// Completes an AI trace record with the final status and outcome.
    /// </summary>
    /// <param name="traceId">The local database ID of the trace.</param>
    /// <param name="status">The final status of the operation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="response">The response object from the AI operation.</param>
    /// <param name="exception">The exception if the operation failed.</param>
    Task CompleteTraceAsync(
        Guid traceId,
        AiTraceStatus status,
        CancellationToken ct,
        object? response = null,
        Exception? exception = null);

    /// <summary>
    /// Records an execution span within a trace.
    /// </summary>
    /// <param name="traceId">The local database ID of the parent trace.</param>
    /// <param name="spanType">The type of span being recorded.</param>
    /// <param name="spanName">The name of the span.</param>
    /// <param name="operation">The operation to execute and record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordSpanAsync(
        Guid traceId,
        AiExecutionSpanType spanType,
        string spanName,
        Func<Task<object?>> operation,
        CancellationToken ct);

    /// <summary>
    /// Retrieves a trace by its local database ID.
    /// </summary>
    /// <param name="id">The unique identifier of the trace.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="includeSpans">Whether to include execution spans in the result.</param>
    /// <returns>The trace if found; otherwise, null.</returns>
    Task<AiTrace?> GetTraceAsync(Guid id, CancellationToken ct, bool includeSpans = false);

    /// <summary>
    /// Retrieves a trace by its OpenTelemetry trace ID.
    /// </summary>
    /// <param name="traceId">The OpenTelemetry trace ID (hex string).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The trace if found; otherwise, null.</returns>
    Task<AiTrace?> GetTraceByTraceIdAsync(string traceId, CancellationToken ct);

    /// <summary>
    /// Retrieves a paginated collection of traces matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the filtered traces and the total count.</returns>
    Task<(IEnumerable<AiTrace>, int Total)> GetTracesPagedAsync(
        AiTraceFilter filter,
        int skip,
        int take,
        CancellationToken ct);

    /// <summary>
    /// Retrieves recent AI traces for a specific entity (e.g., content item).
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="limit">The maximum number of traces to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of recent traces for the entity.</returns>
    Task<IEnumerable<AiTrace>> GetEntityHistoryAsync(
        string entityId,
        string entityType,
        int limit,
        CancellationToken ct);

    /// <summary>
    /// Retrieves all execution spans for a specific trace.
    /// </summary>
    /// <param name="traceId">The local database ID of the trace.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of execution spans ordered by sequence number.</returns>
    Task<IEnumerable<AiExecutionSpan>> GetExecutionSpansAsync(Guid traceId, CancellationToken ct);

    /// <summary>
    /// Deletes a trace and its associated spans.
    /// </summary>
    /// <param name="id">The unique identifier of the trace to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the trace was deleted; otherwise, false.</returns>
    Task<bool> DeleteTraceAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Cleans up old traces based on the configured retention period.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of traces deleted.</returns>
    Task<int> CleanupOldTracesAsync(CancellationToken ct);
}

namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Internal repository interface for AI trace persistence operations.
/// </summary>
internal interface IAiTraceRepository
{
    /// <summary>
    /// Retrieves a trace by its local database ID.
    /// </summary>
    /// <param name="id">The unique identifier of the trace.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The trace if found; otherwise, null.</returns>
    Task<AiTrace?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Retrieves a trace by its OpenTelemetry trace ID.
    /// </summary>
    /// <param name="traceId">The OpenTelemetry trace ID (hex string).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The trace if found; otherwise, null.</returns>
    Task<AiTrace?> GetByTraceIdAsync(string traceId, CancellationToken ct);

    /// <summary>
    /// Retrieves a paginated collection of traces matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the filtered traces and the total count.</returns>
    Task<(IEnumerable<AiTrace>, int Total)> GetPagedAsync(
        AiTraceFilter filter, int skip, int take, CancellationToken ct);

    /// <summary>
    /// Retrieves traces associated with a specific entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="limit">The maximum number of traces to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of traces for the specified entity.</returns>
    Task<IEnumerable<AiTrace>> GetByEntityIdAsync(
        string entityId, string entityType, int limit, CancellationToken ct);

    /// <summary>
    /// Saves a trace to the database (insert or update).
    /// </summary>
    /// <param name="trace">The trace to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved trace with updated ID.</returns>
    Task<AiTrace> SaveAsync(AiTrace trace, CancellationToken ct);

    /// <summary>
    /// Deletes a trace by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the trace to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the trace was deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Deletes all traces older than the specified threshold.
    /// </summary>
    /// <param name="threshold">The date threshold.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of traces deleted.</returns>
    Task<int> DeleteOlderThanAsync(DateTime threshold, CancellationToken ct);
}

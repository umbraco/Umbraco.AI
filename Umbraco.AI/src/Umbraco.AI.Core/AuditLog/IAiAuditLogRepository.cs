namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Internal repository interface for AI audit-log persistence operations.
/// </summary>
internal interface IAiAuditLogRepository
{
    /// <summary>
    /// Retrieves a audit-log by its local database ID.
    /// </summary>
    /// <param name="id">The unique identifier of the audit-log.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The audit-log if found; otherwise, null.</returns>
    Task<AiAuditLog?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Retrieves a paginated collection of traces matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the filtered traces and the total count.</returns>
    Task<(IEnumerable<AiAuditLog>, int Total)> GetPagedAsync(
        AiAuditLogFilter filter, int skip, int take, CancellationToken ct);

    /// <summary>
    /// Retrieves traces associated with a specific entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="limit">The maximum number of traces to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of traces for the specified entity.</returns>
    Task<IEnumerable<AiAuditLog>> GetByEntityIdAsync(
        string entityId, string entityType, int limit, CancellationToken ct);

    /// <summary>
    /// Saves a audit-log to the database (insert or update).
    /// </summary>
    /// <param name="trace">The audit-log to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved audit-log with updated ID.</returns>
    Task<AiAuditLog> SaveAsync(AiAuditLog trace, CancellationToken ct);

    /// <summary>
    /// Deletes a audit-log by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the audit-log to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the audit-log was deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Deletes all traces older than the specified threshold.
    /// </summary>
    /// <param name="threshold">The date threshold.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of traces deleted.</returns>
    Task<int> DeleteOlderThanAsync(DateTime threshold, CancellationToken ct);
}

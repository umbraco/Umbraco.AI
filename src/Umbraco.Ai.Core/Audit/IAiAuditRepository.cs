namespace Umbraco.Ai.Core.Audit;

/// <summary>
/// Internal repository interface for AI audit persistence operations.
/// </summary>
internal interface IAiAuditRepository
{
    /// <summary>
    /// Retrieves a audit by its local database ID.
    /// </summary>
    /// <param name="id">The unique identifier of the audit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The audit if found; otherwise, null.</returns>
    Task<AiAudit?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Retrieves a audit by its OpenTelemetry audit ID.
    /// </summary>
    /// <param name="auditId">The OpenTelemetry audit ID (hex string).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The audit if found; otherwise, null.</returns>
    Task<AiAudit?> GetByTraceIdAsync(string auditId, CancellationToken ct);

    /// <summary>
    /// Retrieves a paginated collection of traces matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the filtered traces and the total count.</returns>
    Task<(IEnumerable<AiAudit>, int Total)> GetPagedAsync(
        AiAuditFilter filter, int skip, int take, CancellationToken ct);

    /// <summary>
    /// Retrieves traces associated with a specific entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="limit">The maximum number of traces to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of traces for the specified entity.</returns>
    Task<IEnumerable<AiAudit>> GetByEntityIdAsync(
        string entityId, string entityType, int limit, CancellationToken ct);

    /// <summary>
    /// Saves a audit to the database (insert or update).
    /// </summary>
    /// <param name="trace">The audit to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved audit with updated ID.</returns>
    Task<AiAudit> SaveAsync(AiAudit trace, CancellationToken ct);

    /// <summary>
    /// Deletes a audit by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the audit to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the audit was deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Deletes all traces older than the specified threshold.
    /// </summary>
    /// <param name="threshold">The date threshold.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of traces deleted.</returns>
    Task<int> DeleteOlderThanAsync(DateTime threshold, CancellationToken ct);
}

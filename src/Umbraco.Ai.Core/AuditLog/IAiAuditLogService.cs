using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Service interface for AI governance tracing operations.
/// Provides methods for recording, retrieving, and managing AI execution traces.
/// </summary>
public interface IAiAuditLogService
{
    /// <summary>
    /// Starts a new AI audit-log record. Completely independent of OpenTelemetry Activity.
    /// Automatically detects parent audit-log from AuditLogScope.Current.
    /// </summary>
    /// <param name="context">The audit-log context containing all metadata.</param>
    /// <param name="metadata">Optional extensible metadata for feature-specific context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created audit-log record.</returns>
    Task<AiAuditLog> StartAuditLogAsync(
        AiAuditContext context,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Starts a new audit-log and immediately creates an AuditLogScope for it.
    /// This is a convenience method that combines StartAuditLogAsync + AuditLogScope.Begin.
    /// Dispose the returned scope when the operation completes.
    /// </summary>
    /// <param name="context">The audit-log context containing all metadata.</param>
    /// <param name="metadata">Optional extensible metadata for feature-specific context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A disposable handle that combines the scope and audit-log.</returns>
    Task<AiAuditScopeHandle> StartAuditLogScopeAsync(
        AiAuditContext context,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Completes an audit-log record with the final result.
    /// </summary>
    /// <param name="audit">The audit-log record to complete.</param>
    /// <param name="response">The response object from the AI operation.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CompleteAuditLogAsync(
        AiAuditLog audit,
        AiAuditResponse? response,
        CancellationToken ct = default);

    /// <summary>
    /// Records an audit-log failure.
    /// </summary>
    /// <param name="audit">The audit-log record that failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordAuditLogFailureAsync(
        AiAuditLog audit,
        Exception exception,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves an audit-log by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the audit-log.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The audit-log if found; otherwise, null.</returns>
    Task<AiAuditLog?> GetAuditLogAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paginated collection of audit-log logs matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the filtered audit-log logs and the total count.</returns>
    Task<(IEnumerable<AiAuditLog>, int Total)> GetAuditLogsPagedAsync(
        AiAuditLogFilter filter,
        int skip,
        int take,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves recent AI audit-log logs for a specific entity (e.g., content item).
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="limit">The maximum number of audit-log logs to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of recent audit-log logs for the entity.</returns>
    Task<IEnumerable<AiAuditLog>> GetEntityHistoryAsync(
        string entityId,
        string entityType,
        int limit,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes an audit-log by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the audit-log to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the audit-log was deleted; otherwise, false.</returns>
    Task<bool> DeleteAuditLogAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Cleans up old audit-log logs based on the configured retention period.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of audit-log logs deleted.</returns>
    Task<int> CleanupOldAuditLogsAsync(CancellationToken ct = default);
}

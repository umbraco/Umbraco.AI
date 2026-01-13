using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Audit;

/// <summary>
/// Service interface for AI governance tracing operations.
/// Provides methods for recording, retrieving, and managing AI execution traces.
/// </summary>
public interface IAiAuditService
{
    /// <summary>
    /// Starts a new AI audit record. Completely independent of OpenTelemetry Activity.
    /// </summary>
    /// <param name="context">The audit context containing all metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created audit record.</returns>
    Task<AiAudit> StartAuditAsync(
        AiAuditContext context,
        CancellationToken ct);

    /// <summary>
    /// Completes an audit record with the final result.
    /// </summary>
    /// <param name="auditId">The local database ID of the audit.</param>
    /// <param name="response">The response object from the AI operation.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CompleteAuditAsync(
        Guid auditId,
        object? response,
        CancellationToken ct);

    /// <summary>
    /// Records an audit failure.
    /// </summary>
    /// <param name="auditId">The local database ID of the audit.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordAuditFailureAsync(
        Guid auditId,
        Exception exception,
        CancellationToken ct);

    /// <summary>
    /// Records an activity within an audit. Caller creates Activity for OpenTelemetry correlation if desired.
    /// </summary>
    /// <param name="auditId">The local database ID of the parent audit.</param>
    /// <param name="activityType">The type of activity being recorded.</param>
    /// <param name="activityName">The name of the activity.</param>
    /// <param name="operation">The operation to execute and record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordActivityAsync(
        Guid auditId,
        AiAuditActivityType activityType,
        string activityName,
        Func<Task<object?>> operation,
        CancellationToken ct);

    /// <summary>
    /// Retrieves a audit by its local database ID.
    /// </summary>
    /// <param name="id">The unique identifier of the audit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="includeActivities">Whether to include execution Activities in the result.</param>
    /// <returns>The audit if found; otherwise, null.</returns>
    Task<AiAudit?> GetAuditAsync(Guid id, CancellationToken ct, bool includeActivities = false);

    /// <summary>
    /// Retrieves a audit by its OpenTelemetry audit ID.
    /// </summary>
    /// <param name="auditId">The OpenTelemetry audit ID (hex string).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The audit if found; otherwise, null.</returns>
    Task<AiAudit?> GetAuditByTraceIdAsync(string auditId, CancellationToken ct);

    /// <summary>
    /// Retrieves a paginated collection of traces matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the filtered traces and the total count.</returns>
    Task<(IEnumerable<AiAudit>, int Total)> GetAuditsPagedAsync(
        AiAuditFilter filter,
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
    Task<IEnumerable<AiAudit>> GetEntityHistoryAsync(
        string entityId,
        string entityType,
        int limit,
        CancellationToken ct);

    /// <summary>
    /// Retrieves all execution Activities for a specific audit.
    /// </summary>
    /// <param name="auditId">The local database ID of the audit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of execution Activities ordered by sequence number.</returns>
    Task<IEnumerable<AiAuditActivity>> GetAuditActivitiesAsync(Guid auditId, CancellationToken ct);

    /// <summary>
    /// Deletes a audit and its associated Activities.
    /// </summary>
    /// <param name="id">The unique identifier of the audit to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the audit was deleted; otherwise, false.</returns>
    Task<bool> DeleteAuditAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Cleans up old traces based on the configured retention period.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of traces deleted.</returns>
    Task<int> CleanupOldAuditsAsync(CancellationToken ct);
}

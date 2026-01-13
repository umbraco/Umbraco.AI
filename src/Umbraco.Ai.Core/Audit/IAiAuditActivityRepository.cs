namespace Umbraco.Ai.Core.Audit;

/// <summary>
/// Internal repository interface for AI execution span persistence operations.
/// </summary>
internal interface IAiAuditActivityRepository
{
    /// <summary>
    /// Retrieves all activities for a specific audit.
    /// </summary>
    /// <param name="auditId">The audit ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of activities ordered by sequence number.</returns>
    Task<IEnumerable<AiAuditActivity>> GetByAuditIdAsync(Guid auditId, CancellationToken ct);

    /// <summary>
    /// Saves a single execution span to the database.
    /// </summary>
    /// <param name="span">The span to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved span with updated ID.</returns>
    Task<AiAuditActivity> SaveAsync(AiAuditActivity span, CancellationToken ct);

    /// <summary>
    /// Saves multiple execution Activities in a single batch operation.
    /// </summary>
    /// <param name="activities">The Activities to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveBatchAsync(IEnumerable<AiAuditActivity> activities, CancellationToken ct);
}

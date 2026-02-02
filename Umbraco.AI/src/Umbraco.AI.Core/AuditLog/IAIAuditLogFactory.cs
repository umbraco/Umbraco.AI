namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Factory for creating AIAuditLog instances with proper dependency injection.
/// </summary>
internal interface IAIAuditLogFactory
{
    /// <summary>
    /// Creates a new AIAuditLog instance from the given AIAuditContext.
    /// </summary>
    /// <param name="context">The AIAuditContext containing operation details.</param>
    /// <param name="metadata">Optional metadata to include in the audit-log.</param>
    /// <param name="parentId">Optional parent audit-log ID if this log is part of a nested operation.</param>
    /// <returns>A new AIAuditLog instance ready to be persisted.</returns>
    AIAuditLog Create(
        AIAuditContext context,
        IReadOnlyDictionary<string, string>? metadata = null,
        Guid? parentId = null);
}

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Factory for creating AiAuditLog instances with proper dependency injection.
/// </summary>
internal interface IAiAuditLogFactory
{
    /// <summary>
    /// Creates a new AiAuditLog instance from the given AiAuditContext.
    /// </summary>
    /// <param name="context">The AiAuditContext containing operation details.</param>
    /// <param name="metadata">Optional metadata to include in the audit-log.</param>
    /// <param name="parentId">Optional parent audit-log ID if this log is part of a nested operation.</param>
    /// <returns>A new AiAuditLog instance ready to be persisted.</returns>
    AiAuditLog Create(
        AiAuditContext context,
        IReadOnlyDictionary<string, string>? metadata = null,
        Guid? parentId = null);
}

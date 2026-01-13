namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Combines an AuditLogScope with its associated AiAuditLog for convenience.
/// Returned by StartAuditLogScopeAsync.
/// Dispose this handle to automatically restore the previous audit-log scope.
/// </summary>
public sealed class AiAuditScopeHandle : IDisposable
{
    /// <summary>
    /// Gets the underlying audit-log scope.
    /// </summary>
    public AiAuditScope Scope { get; }

    /// <summary>
    /// Gets the audit-log associated with this scope.
    /// </summary>
    public AiAuditLog AuditLog { get; }

    internal AiAuditScopeHandle(AiAuditScope scope, AiAuditLog audit)
    {
        Scope = scope;
        AuditLog = audit;
    }

    /// <summary>
    /// Disposes the underlying scope, restoring the previous audit-log scope.
    /// </summary>
    public void Dispose() => Scope.Dispose();
}

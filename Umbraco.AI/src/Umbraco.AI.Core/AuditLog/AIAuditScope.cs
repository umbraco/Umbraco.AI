namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Represents an ambient audit-log scope that automatically tracks parent-child relationships
/// for nested AI operations (e.g., agent calling another agent).
/// Uses AsyncLocal to flow across async boundaries.
/// </summary>
public sealed class AIAuditScope : IDisposable
{
    private static readonly AsyncLocal<AIAuditScope?> _current = new();

    /// <summary>
    /// Gets the current active audit-log scope, if any.
    /// </summary>
    public static AIAuditScope? Current => _current.Value;

    /// <summary>
    /// Gets the audit-log ID for this scope.
    /// </summary>
    public Guid AuditLogId { get; }

    private readonly AIAuditScope? _previousScope;

    private AIAuditScope(Guid auditLogId)
    {
        AuditLogId = auditLogId;
        _previousScope = _current.Value;
        _current.Value = this;
    }

    /// <summary>
    /// Begins a new audit-log scope. If called within another scope, the parent relationship
    /// can be obtained via <see cref="Current"/>.<see cref="AuditLogId"/>.
    /// </summary>
    /// <param name="auditLogId">The audit-log ID for this scope.</param>
    /// <returns>A disposable scope that restores the previous scope on disposal.</returns>
    public static AIAuditScope Begin(Guid auditLogId)
    {
        return new AIAuditScope(auditLogId);
    }

    /// <summary>
    /// Restores the previous scope.
    /// </summary>
    public void Dispose()
    {
        _current.Value = _previousScope;
    }
}

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Specifies the level of detail to capture for AI traces.
/// </summary>
public enum AiAuditLogDetailLevel
{
    /// <summary>
    /// Minimal detail - only core audit-log information (user, model, tokens, status).
    /// </summary>
    AuditLog = 0,

    /// <summary>
    /// Detailed information only for failed operations.
    /// </summary>
    FailuresOnly = 1,

    /// <summary>
    /// Sampled detailed information based on sampling rate.
    /// </summary>
    Sampled = 2,

    /// <summary>
    /// Full detailed information for all operations.
    /// </summary>
    Full = 3
}

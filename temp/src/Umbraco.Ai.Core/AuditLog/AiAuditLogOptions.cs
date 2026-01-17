namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Configuration options for AI auditing.
/// </summary>
public class AiAuditLogOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether audit-log logging is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of days to retain audit-log records before cleanup.
    /// Default is 14 days.
    /// </summary>
    public int RetentionDays { get; set; } = 14;

    /// <summary>
    /// Gets or sets the detail level for capturing audit-log information.
    /// Default is FailuresOnly.
    /// </summary>
    public AiAuditLogDetailLevel DetailLevel { get; set; } = AiAuditLogDetailLevel.FailuresOnly;

    /// <summary>
    /// Gets or sets a value indicating whether to persist prompt snapshots.
    /// Default is false for privacy reasons.
    /// </summary>
    public bool PersistPrompts { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to persist response snapshots.
    /// Default is false for privacy reasons.
    /// </summary>
    public bool PersistResponses { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to persist detailed failure information.
    /// Default is true.
    /// </summary>
    public bool PersistFailureDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of regex patterns for redacting sensitive data.
    /// </summary>
    public List<string> RedactionPatterns { get; set; } = new();
}

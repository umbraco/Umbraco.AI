namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Configuration options for AI auditing.
/// </summary>
public class AIAuditLogOptions
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
    /// Gets or sets a value indicating whether to persist prompt snapshots.
    /// Default is true.
    /// </summary>
    public bool PersistPrompts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to persist response snapshots.
    /// Default is true.
    /// </summary>
    public bool PersistResponses { get; set; } = true;

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

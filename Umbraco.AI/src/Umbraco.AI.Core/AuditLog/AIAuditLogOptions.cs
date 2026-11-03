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
    /// Gets or sets how many minutes an audit-log may stay <see cref="AIAuditLogStatus.Running"/> before
    /// cleanup treats it as abandoned (e.g. the process stopped mid-call) and marks it as failed.
    /// Set to 0 or less to disable. Default is 60 minutes.
    /// </summary>
    public int StaleRunningTimeoutMinutes { get; set; } = 60;

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
    /// Gets or sets the maximum number of characters of each tool call's arguments to keep in prompt and
    /// response snapshots. Longer arguments are truncated. Set to 0 or less to keep them in full.
    /// Default is 1000.
    /// </summary>
    public int MaxToolArgumentsLength { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum number of characters of each tool result to keep in prompt and response
    /// snapshots. Longer results are truncated. Set to 0 or less to keep them in full.
    /// Default is 4000.
    /// </summary>
    public int MaxToolResultLength { get; set; } = 4000;

    /// <summary>
    /// Gets or sets the list of regex patterns for redacting sensitive data.
    /// </summary>
    public List<string> RedactionPatterns { get; set; } = new();
}

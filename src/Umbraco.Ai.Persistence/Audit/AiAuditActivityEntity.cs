namespace Umbraco.Ai.Persistence.Audit;

/// <summary>
/// EF Core entity for AI execution span records.
/// </summary>
public class AiAuditActivityEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this span.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the parent audit ID.
    /// </summary>
    public Guid AuditId { get; set; }

    /// <summary>
    /// Gets or sets the OpenTelemetry span ID (hex string).
    /// </summary>
    public string ActivityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent activity ID.
    /// </summary>
    public string? ParentActivityId { get; set; }

    /// <summary>
    /// Gets or sets the activity name.
    /// </summary>
    public string ActivityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the activity type.
    /// </summary>
    public int ActivityType { get; set; }

    /// <summary>
    /// Gets or sets the sequence number.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets the input data.
    /// </summary>
    public string? InputData { get; set; }

    /// <summary>
    /// Gets or sets the output data.
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Gets or sets the error data.
    /// </summary>
    public string? ErrorData { get; set; }

    /// <summary>
    /// Gets or sets the retry count.
    /// </summary>
    public int? RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the tokens used.
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Navigation property to the parent audit.
    /// </summary>
    public AiAuditEntity? Audit { get; set; }
}

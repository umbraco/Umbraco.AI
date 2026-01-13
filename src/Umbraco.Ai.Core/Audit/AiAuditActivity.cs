namespace Umbraco.Ai.Core.Audit;

/// <summary>
/// Represents detailed execution information for a step within an AI audit.
/// </summary>
public sealed class AiAuditActivity
{
    /// <summary>
    /// Gets the unique identifier for this span.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// Gets the identifier of the parent audit.
    /// </summary>
    public Guid AuditId { get; init; }

    /// <summary>
    /// Gets the name of this activity.
    /// </summary>
    public string ActivityName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of this activity.
    /// </summary>
    public AiAuditActivityType ActivityType { get; init; }

    /// <summary>
    /// Gets the sequence number of this span within the audit.
    /// </summary>
    public int SequenceNumber { get; init; }

    /// <summary>
    /// Gets the start time of this span.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Gets or sets the end time of this span.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets the duration of this span, if completed.
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

    /// <summary>
    /// Gets or sets the status of this span.
    /// </summary>
    public AiAuditActivityStatus Status { get; set; }

    /// <summary>
    /// Gets the input data for this span.
    /// </summary>
    public string? InputData { get; init; }

    /// <summary>
    /// Gets or sets the output data from this span.
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Gets or sets error data if this span failed.
    /// </summary>
    public string? ErrorData { get; set; }

    /// <summary>
    /// Gets or sets the number of retries attempted for this span.
    /// </summary>
    public int? RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens used by this span, if applicable.
    /// </summary>
    public int? TokensUsed { get; set; }
}

namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Represents detailed execution information for a step within an AI trace.
/// </summary>
public sealed class AiExecutionSpan
{
    /// <summary>
    /// Gets the unique identifier for this span.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// Gets the identifier of the parent trace.
    /// </summary>
    public Guid TraceId { get; init; }

    /// <summary>
    /// Gets the OpenTelemetry span ID.
    /// </summary>
    public string SpanId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the parent span ID, if this span is nested.
    /// </summary>
    public string? ParentSpanId { get; init; }

    /// <summary>
    /// Gets the name of this span.
    /// </summary>
    public string SpanName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of this span.
    /// </summary>
    public AiExecutionSpanType SpanType { get; init; }

    /// <summary>
    /// Gets the sequence number of this span within the trace.
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
    public AiExecutionSpanStatus Status { get; set; }

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

namespace Umbraco.Ai.Persistence.Governance;

/// <summary>
/// EF Core entity for AI execution span records.
/// </summary>
public class AiExecutionSpanEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this span.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the parent trace ID.
    /// </summary>
    public Guid TraceId { get; set; }

    /// <summary>
    /// Gets or sets the OpenTelemetry span ID (hex string).
    /// </summary>
    public string SpanId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent span ID.
    /// </summary>
    public string? ParentSpanId { get; set; }

    /// <summary>
    /// Gets or sets the span name.
    /// </summary>
    public string SpanName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the span type.
    /// </summary>
    public int SpanType { get; set; }

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
    /// Navigation property to the parent trace.
    /// </summary>
    public AiTraceEntity? Trace { get; set; }
}

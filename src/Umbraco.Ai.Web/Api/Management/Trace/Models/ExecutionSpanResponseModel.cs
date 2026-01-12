using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Trace.Models;

/// <summary>
/// Response model for an execution span (detailed step within a trace).
/// </summary>
public class ExecutionSpanResponseModel
{
    /// <summary>
    /// The unique identifier of the span.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The trace ID this span belongs to.
    /// </summary>
    [Required]
    public Guid TraceId { get; set; }

    /// <summary>
    /// The OpenTelemetry SpanId.
    /// </summary>
    [Required]
    public string SpanId { get; set; } = string.Empty;

    /// <summary>
    /// The parent span ID if this is a child span.
    /// </summary>
    public string? ParentSpanId { get; set; }

    /// <summary>
    /// The name of this span (e.g., "Profile Resolution", "Model Call").
    /// </summary>
    [Required]
    public string SpanName { get; set; } = string.Empty;

    /// <summary>
    /// The type of this span (e.g., ProfileResolution, ContextResolution, ModelCall).
    /// </summary>
    [Required]
    public string SpanType { get; set; } = string.Empty;

    /// <summary>
    /// The sequence number of this span within the trace.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The start time of the span.
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// The end time of the span.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// The duration of the span in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// The status of the span (e.g., Running, Succeeded, Failed, Skipped).
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Input data for this span if available.
    /// </summary>
    public string? InputData { get; set; }

    /// <summary>
    /// Output data for this span if available.
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Error data if the span failed.
    /// </summary>
    public string? ErrorData { get; set; }

    /// <summary>
    /// Number of retry attempts for this span.
    /// </summary>
    public int? RetryCount { get; set; }

    /// <summary>
    /// Number of tokens used by this span.
    /// </summary>
    public int? TokensUsed { get; set; }
}

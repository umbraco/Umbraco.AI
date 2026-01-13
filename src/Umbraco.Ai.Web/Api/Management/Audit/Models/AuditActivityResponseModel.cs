using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Audit.Models;

/// <summary>
/// Response model for an audit activity (detailed step within an audit).
/// </summary>
public class AuditActivityResponseModel
{
    /// <summary>
    /// The unique identifier of the activity.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The audit ID this activity belongs to.
    /// </summary>
    [Required]
    public Guid AuditId { get; set; }

    /// <summary>
    /// The name of this activity (e.g., "Tool Execution", "Context Resolution").
    /// </summary>
    [Required]
    public string ActivityName { get; set; } = string.Empty;

    /// <summary>
    /// The type of this activity (e.g., ToolExecution, ContextResolution).
    /// </summary>
    [Required]
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// The sequence number of this span within the audit.
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

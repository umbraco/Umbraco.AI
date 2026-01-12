using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Trace.Models;

/// <summary>
/// Detailed response model for a trace (used in detail views).
/// </summary>
public class TraceDetailResponseModel : TraceItemResponseModel
{
    /// <summary>
    /// The OpenTelemetry SpanId.
    /// </summary>
    [Required]
    public string SpanId { get; set; } = string.Empty;

    /// <summary>
    /// The end time of the trace.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// The entity type if this trace is associated with a specific entity.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// The profile ID used for this request.
    /// </summary>
    [Required]
    public Guid ProfileId { get; set; }

    /// <summary>
    /// The profile alias.
    /// </summary>
    [Required]
    public string ProfileAlias { get; set; } = string.Empty;

    /// <summary>
    /// Error category if the trace failed.
    /// </summary>
    public string? ErrorCategory { get; set; }

    /// <summary>
    /// Total number of tokens consumed (input + output).
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// Snapshot of the prompt if configured to persist.
    /// </summary>
    public string? PromptSnapshot { get; set; }

    /// <summary>
    /// Snapshot of the response if configured to persist.
    /// </summary>
    public string? ResponseSnapshot { get; set; }

    /// <summary>
    /// Detail level of this trace record.
    /// </summary>
    [Required]
    public string DetailLevel { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this trace has execution spans available.
    /// </summary>
    public bool HasSpans { get; set; }
}

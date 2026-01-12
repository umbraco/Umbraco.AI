using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Trace.Models;

/// <summary>
/// Lightweight response model for a trace item (used in lists).
/// </summary>
public class TraceItemResponseModel
{
    /// <summary>
    /// The unique identifier of the trace.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The OpenTelemetry TraceId.
    /// </summary>
    [Required]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// The start time of the trace.
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// The duration of the trace in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// The status of the trace (e.g., Running, Succeeded, Failed).
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The user ID who initiated the request.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The user name if available.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The entity ID if this trace is associated with a specific entity (e.g., content item).
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// The operation type (e.g., "chat.complete", "embedding.generate").
    /// </summary>
    [Required]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// The model ID used for this request.
    /// </summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// The provider ID (e.g., "openai", "azure-openai").
    /// </summary>
    [Required]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Number of input tokens consumed.
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Error message if the trace failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

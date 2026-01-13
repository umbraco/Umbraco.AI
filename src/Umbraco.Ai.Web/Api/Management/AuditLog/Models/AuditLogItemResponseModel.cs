using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.AuditLog.Models;

/// <summary>
/// Lightweight response model for a audit-log item (used in lists).
/// </summary>
public class AuditLogItemResponseModel
{
    /// <summary>
    /// The unique identifier of the audit-log.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The start time of the audit-log.
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// The duration of the audit-log in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// The status of the audit-log (e.g., Running, Succeeded, Failed).
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The user ID who initiated the request.
    /// </summary>
    [Required]
    public string? UserId { get; set; }

    /// <summary>
    /// The user name if available.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The entity ID if this audit-log is associated with a specific entity (e.g., content item).
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// The capability type (e.g., "Chat", "Embedding").
    /// </summary>
    [Required]
    public string Capability { get; set; } = string.Empty;

    /// <summary>
    /// The profile ID used for this request.
    /// </summary>
    [Required]
    public string ProfileId { get; set; } = string.Empty;

    /// <summary>
    /// The profile alias used for this request.
    /// </summary>
    [Required]
    public string ProfileAlias { get; set; } = string.Empty;

    /// <summary>
    /// The provider ID (e.g., "openai", "azure-openai").
    /// </summary>
    [Required]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// The model ID used for this request.
    /// </summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// The feature type that initiated this operation (e.g., "prompt", "agent").
    /// </summary>
    public string? FeatureType { get; set; }

    /// <summary>
    /// The feature ID (prompt or agent ID) that initiated this operation.
    /// </summary>
    public Guid? FeatureId { get; set; }

    /// <summary>
    /// The parent audit-log ID if this is a child audit-log (e.g., agent-to-agent calls).
    /// </summary>
    public Guid? ParentAuditLogId { get; set; }

    /// <summary>
    /// Extensible metadata dictionary for feature-specific context (e.g., AgentRunId, ThreadId).
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Number of input tokens consumed.
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Error message if the audit-log failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

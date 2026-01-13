using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Audit.Models;

/// <summary>
/// Detailed response model for a audit (used in detail views).
/// </summary>
public class AuditDetailResponseModel : AuditItemResponseModel
{
    /// <summary>
    /// The end time of the audit.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// The entity type if this audit is associated with a specific entity.
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
    /// Error category if the audit failed.
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
    /// Detail level of this audit record.
    /// </summary>
    [Required]
    public string DetailLevel { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this audit has audit actvitys available.
    /// </summary>
    public bool HasSpans { get; set; }
}

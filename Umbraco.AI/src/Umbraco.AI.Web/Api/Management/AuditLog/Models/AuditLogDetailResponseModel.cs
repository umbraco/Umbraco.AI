using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.AuditLog.Models;

/// <summary>
/// Detailed response model for a audit-log (used in detail views).
/// </summary>
public class AuditLogDetailResponseModel : AuditLogItemResponseModel
{
    /// <summary>
    /// The end time of the audit-log.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// The entity type if this audit-log is associated with a specific entity.
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
    /// Error category if the audit-log failed.
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
    /// Extensible metadata dictionary for feature-specific context (e.g., AgentRunId, ThreadId).
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>>? Metadata { get; set; }
}

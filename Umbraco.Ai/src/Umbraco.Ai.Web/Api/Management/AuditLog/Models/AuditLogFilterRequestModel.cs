namespace Umbraco.Ai.Web.Api.Management.AuditLog.Models;

/// <summary>
/// Request model for filtering audits.
/// </summary>
public class AuditLogFilterRequestModel
{
    /// <summary>
    /// Filter by audit-log status (e.g., Running, Succeeded, Failed).
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by profile ID.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Filter by provider ID.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Filter by capability (e.g., Chat, Embedding).
    /// </summary>
    public string? Capability { get; set; }

    /// <summary>
    /// Filter by feature type (e.g., "prompt", "agent").
    /// </summary>
    public string? FeatureType { get; set; }

    /// <summary>
    /// Filter by feature ID.
    /// </summary>
    public Guid? FeatureId { get; set; }

    /// <summary>
    /// Filter by entity ID.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Filter by entity type.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Filter by parent audit-log ID (to find child audits).
    /// </summary>
    public Guid? ParentAuditLogId { get; set; }

    /// <summary>
    /// Filter by start date (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by end date (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Search text to filter by (searches across multiple fields).
    /// </summary>
    public string? SearchText { get; set; }
}

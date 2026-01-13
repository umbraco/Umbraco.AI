namespace Umbraco.Ai.Web.Api.Management.Audit.Models;

/// <summary>
/// Request model for filtering audits.
/// </summary>
public class AuditFilterRequestModel
{
    /// <summary>
    /// Filter by audit status (e.g., Running, Succeeded, Failed).
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
    /// Filter by entity ID.
    /// </summary>
    public string? EntityId { get; set; }

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

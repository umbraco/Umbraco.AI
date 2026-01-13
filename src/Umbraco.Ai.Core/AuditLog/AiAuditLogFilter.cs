using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Filter criteria for querying AI traces.
/// </summary>
public class AiAuditLogFilter
{
    /// <summary>
    /// Gets or sets the status to filter by.
    /// </summary>
    public AiAuditLogStatus? Status { get; init; }

    /// <summary>
    /// Gets or sets the user ID to filter by.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or sets the profile ID to filter by.
    /// </summary>
    public Guid? ProfileId { get; init; }

    /// <summary>
    /// Gets or sets the provider ID to filter by.
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Gets or sets the AI capability to filter by (Chat, Embedding, etc.).
    /// </summary>
    public AiCapability? Capability { get; init; }

    /// <summary>
    /// Gets or sets the feature type to filter by (e.g., "prompt", "agent").
    /// </summary>
    public string? FeatureType { get; init; }

    /// <summary>
    /// Gets or sets the feature ID to filter by.
    /// </summary>
    public Guid? FeatureId { get; init; }

    /// <summary>
    /// Gets or sets the entity ID to filter by.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Gets or sets the entity type to filter by (e.g., "content", "media").
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Gets or sets the parent audit-log ID to filter by (for finding child audits).
    /// </summary>
    public Guid? ParentAuditLogId { get; init; }

    /// <summary>
    /// Gets or sets the start date for the date range filter.
    /// </summary>
    public DateTime? FromDate { get; init; }

    /// <summary>
    /// Gets or sets the end date for the date range filter.
    /// </summary>
    public DateTime? ToDate { get; init; }

    /// <summary>
    /// Gets or sets the search text for filtering by model, error message, etc.
    /// </summary>
    public string? SearchText { get; init; }
}

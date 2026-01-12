namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Filter criteria for querying AI traces.
/// </summary>
public class AiTraceFilter
{
    /// <summary>
    /// Gets or sets the status to filter by.
    /// </summary>
    public AiTraceStatus? Status { get; init; }

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
    /// Gets or sets the entity ID to filter by.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Gets or sets the start date for the date range filter.
    /// </summary>
    public DateTime? FromDate { get; init; }

    /// <summary>
    /// Gets or sets the end date for the date range filter.
    /// </summary>
    public DateTime? ToDate { get; init; }

    /// <summary>
    /// Gets or sets the search text for filtering by operation type, model, error message, etc.
    /// </summary>
    public string? SearchText { get; init; }
}

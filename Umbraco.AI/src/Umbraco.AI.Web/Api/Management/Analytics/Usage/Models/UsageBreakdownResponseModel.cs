using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Analytics.Usage.Models;

/// <summary>
/// Response model for one page of usage rows broken down by a dimension.
/// </summary>
public class UsageBreakdownResponseModel
{
    /// <summary>
    /// Gets or sets how many rows match the search, before paging.
    /// </summary>
    [Required]
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the rows of the page.
    /// </summary>
    [Required]
    public IEnumerable<UsageBreakdownRowModel> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the counts for the whole range, whatever the search and page.
    /// </summary>
    [Required]
    public UsageCountsModel Totals { get; set; } = new();

    /// <summary>
    /// Gets or sets the counts for the whole comparison range. Null when no comparison range was sent.
    /// </summary>
    public UsageCountsModel? PreviousTotals { get; set; }
}

/// <summary>
/// One row of a usage breakdown.
/// </summary>
public class UsageBreakdownRowModel
{
    /// <summary>
    /// Gets or sets the row's identity: a provider ID, model ID, profile ID or user ID, or "unknown"
    /// (provider, model, profile) or "anonymous" (user) when usage has none recorded.
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the latest friendly name (profile alias, user name). Null for providers and models.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the row's counts for the range.
    /// </summary>
    [Required]
    public UsageCountsModel Current { get; set; } = new();

    /// <summary>
    /// Gets or sets the row's counts for the comparison range (zeros when it had no usage).
    /// Null when no comparison range was sent.
    /// </summary>
    public UsageCountsModel? Previous { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Analytics.Models;

/// <summary>
/// Response model for usage breakdown by a specific dimension.
/// </summary>
public class UsageBreakdownItemModel
{
    /// <summary>
    /// Gets or sets the dimension value (e.g., provider name, model ID, profile alias).
    /// </summary>
    [Required]
    public string Dimension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of requests for this dimension.
    /// </summary>
    [Required]
    public int RequestCount { get; set; }

    /// <summary>
    /// Gets or sets the total tokens consumed for this dimension.
    /// </summary>
    [Required]
    public long TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total requests represented by this dimension.
    /// </summary>
    [Required]
    public double Percentage { get; set; }
}

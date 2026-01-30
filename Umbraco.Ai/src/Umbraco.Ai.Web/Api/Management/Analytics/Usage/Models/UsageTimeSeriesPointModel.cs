using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Analytics.Usage.Models;

/// <summary>
/// Response model for a single point in a usage time series.
/// </summary>
public class UsageTimeSeriesPointModel
{
    /// <summary>
    /// Gets or sets the timestamp for this data point (hour or day start).
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the number of requests in this time period.
    /// </summary>
    [Required]
    public int RequestCount { get; set; }

    /// <summary>
    /// Gets or sets the total tokens consumed in this time period.
    /// </summary>
    [Required]
    public long TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the input tokens consumed in this time period.
    /// </summary>
    [Required]
    public long InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the output tokens consumed in this time period.
    /// </summary>
    [Required]
    public long OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of successful requests.
    /// </summary>
    [Required]
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed requests.
    /// </summary>
    [Required]
    public int FailureCount { get; set; }
}

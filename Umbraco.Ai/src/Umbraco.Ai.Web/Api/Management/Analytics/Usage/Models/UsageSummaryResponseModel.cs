using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Analytics.Usage.Models;

/// <summary>
/// Response model containing summary statistics for a time period.
/// </summary>
public class UsageSummaryResponseModel
{
    /// <summary>
    /// Gets or sets the total number of AI requests.
    /// </summary>
    [Required]
    public int TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the total number of input tokens consumed.
    /// </summary>
    [Required]
    public long InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of output tokens generated.
    /// </summary>
    [Required]
    public long OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens (input + output).
    /// </summary>
    [Required]
    public long TotalTokens { get; set; }

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

    /// <summary>
    /// Gets or sets the success rate as a decimal (0.0 to 1.0).
    /// </summary>
    [Required]
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average request duration in milliseconds.
    /// </summary>
    [Required]
    public int AverageDurationMs { get; set; }
}

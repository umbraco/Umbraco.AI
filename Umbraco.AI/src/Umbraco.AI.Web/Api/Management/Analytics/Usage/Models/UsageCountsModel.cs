using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Analytics.Usage.Models;

/// <summary>
/// Raw usage counts for a bucket, a range or a breakdown row. Rates and averages are left to the client.
/// </summary>
public class UsageCountsModel
{
    /// <summary>
    /// Gets or sets the number of requests.
    /// </summary>
    [Required]
    public int Requests { get; set; }

    /// <summary>
    /// Gets or sets how many of <see cref="Requests"/> were made while another tracked AI operation was running.
    /// </summary>
    [Required]
    public int NestedRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of successful requests.
    /// </summary>
    [Required]
    public int Succeeded { get; set; }

    /// <summary>
    /// Gets or sets the number of failed or blocked requests.
    /// </summary>
    [Required]
    public int Failed { get; set; }

    /// <summary>
    /// Gets or sets the input tokens consumed.
    /// </summary>
    [Required]
    public long InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the portion of <see cref="InputTokens"/> served from a provider's prompt cache.
    /// Null when no provider reports it.
    /// </summary>
    public long? CachedInputTokens { get; set; }

    /// <summary>
    /// Gets or sets the output tokens generated.
    /// </summary>
    [Required]
    public long OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total tokens (input + output).
    /// </summary>
    [Required]
    public long TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the time spent in requests, in milliseconds.
    /// </summary>
    [Required]
    public long DurationMs { get; set; }
}

using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Web.Api.Management.Test.Models;

namespace Umbraco.AI.Web.Api.Management.TestRun.Models;

/// <summary>
/// Response model for variation comparison within an execution.
/// </summary>
public class TestVariationComparisonResponseModel
{
    /// <summary>
    /// Name of the source variation ("Default" or variation name).
    /// </summary>
    [Required]
    public string SourceVariationName { get; set; } = string.Empty;

    /// <summary>
    /// Metrics from the source variation.
    /// </summary>
    [Required]
    public TestMetricsResponseModel SourceMetrics { get; set; } = null!;

    /// <summary>
    /// Name of the comparison variation.
    /// </summary>
    [Required]
    public string ComparisonVariationName { get; set; } = string.Empty;

    /// <summary>
    /// Metrics from the comparison variation.
    /// </summary>
    [Required]
    public TestMetricsResponseModel ComparisonMetrics { get; set; } = null!;

    /// <summary>
    /// Difference in pass rate (comparison - source).
    /// Positive = comparison has higher pass rate.
    /// </summary>
    public double PassRateDelta { get; set; }

    /// <summary>
    /// Difference in average duration in milliseconds (comparison - source).
    /// Positive = comparison is slower.
    /// </summary>
    public double AverageDurationDeltaMs { get; set; }

    /// <summary>
    /// Whether the comparison represents a regression from the source.
    /// </summary>
    public bool IsRegression { get; set; }

    /// <summary>
    /// Whether the comparison represents an improvement from the source.
    /// </summary>
    public bool IsImprovement { get; set; }
}

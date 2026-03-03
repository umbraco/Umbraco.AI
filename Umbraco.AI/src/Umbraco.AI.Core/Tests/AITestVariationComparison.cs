namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Comparison between two variation groups within a test execution.
/// Used for pairwise drill-down (default vs variation, or variation vs variation).
/// </summary>
public sealed class AITestVariationComparison
{
    /// <summary>
    /// Name of the source variation ("Default" or variation name).
    /// </summary>
    public required string SourceVariationName { get; set; }

    /// <summary>
    /// Metrics from the source variation.
    /// </summary>
    public required AITestMetrics SourceMetrics { get; set; }

    /// <summary>
    /// Name of the comparison variation.
    /// </summary>
    public required string ComparisonVariationName { get; set; }

    /// <summary>
    /// Metrics from the comparison variation.
    /// </summary>
    public required AITestMetrics ComparisonMetrics { get; set; }

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

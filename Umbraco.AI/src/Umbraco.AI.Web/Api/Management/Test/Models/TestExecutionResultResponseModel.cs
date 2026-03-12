namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for a test execution result with per-variation metrics.
/// </summary>
public class TestExecutionResultResponseModel
{
    /// <summary>
    /// The test ID that was executed.
    /// </summary>
    public Guid TestId { get; set; }

    /// <summary>
    /// The execution ID grouping all runs.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Optional batch ID if part of a batch.
    /// </summary>
    public Guid? BatchId { get; set; }

    /// <summary>
    /// Metrics from the default configuration runs.
    /// </summary>
    public TestMetricsResponseModel DefaultMetrics { get; set; } = null!;

    /// <summary>
    /// Metrics for each variation.
    /// </summary>
    public IReadOnlyList<TestVariationMetricsResponseModel> VariationMetrics { get; set; } = [];

    /// <summary>
    /// Aggregate metrics across all runs (default + variations).
    /// </summary>
    public TestMetricsResponseModel AggregateMetrics { get; set; } = null!;
}

/// <summary>
/// Metrics for a single variation within a test execution.
/// </summary>
public class TestVariationMetricsResponseModel
{
    /// <summary>
    /// The variation ID.
    /// </summary>
    public Guid VariationId { get; set; }

    /// <summary>
    /// The variation name.
    /// </summary>
    public string VariationName { get; set; } = string.Empty;

    /// <summary>
    /// Metrics for this variation's runs.
    /// </summary>
    public TestMetricsResponseModel Metrics { get; set; } = null!;
}

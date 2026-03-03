namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Wraps per-variation metrics from a single test execution.
/// Contains the default config metrics, each variation's metrics, and aggregate metrics.
/// </summary>
public sealed class AITestExecutionResult
{
    /// <summary>
    /// The test ID that was executed.
    /// </summary>
    public required Guid TestId { get; set; }

    /// <summary>
    /// The execution ID grouping all runs (default + variations).
    /// </summary>
    public required Guid ExecutionId { get; set; }

    /// <summary>
    /// Optional batch ID if this execution was part of a batch.
    /// </summary>
    public Guid? BatchId { get; set; }

    /// <summary>
    /// Metrics from the default configuration runs (VariationId=null).
    /// </summary>
    public required AITestMetrics DefaultMetrics { get; set; }

    /// <summary>
    /// Metrics for each variation that was executed.
    /// </summary>
    public IReadOnlyList<AITestVariationMetrics> VariationMetrics { get; set; } = [];

    /// <summary>
    /// Aggregate metrics across all runs (default + all variations).
    /// </summary>
    public required AITestMetrics AggregateMetrics { get; set; }
}

/// <summary>
/// Metrics for a single variation within a test execution.
/// </summary>
public sealed class AITestVariationMetrics
{
    /// <summary>
    /// The variation ID.
    /// </summary>
    public required Guid VariationId { get; set; }

    /// <summary>
    /// The variation name (denormalized for display).
    /// </summary>
    public required string VariationName { get; set; }

    /// <summary>
    /// Metrics for this variation's runs.
    /// </summary>
    public required AITestMetrics Metrics { get; set; }
}

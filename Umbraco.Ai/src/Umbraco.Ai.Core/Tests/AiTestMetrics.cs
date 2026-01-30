namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Metrics calculated from multiple test runs to measure non-determinism.
/// Following Anthropic's eval framework with pass@k and pass^k metrics.
/// </summary>
public sealed class AiTestMetrics
{
    /// <summary>
    /// The test ID these metrics are for.
    /// </summary>
    public Guid TestId { get; set; }

    /// <summary>
    /// The total number of runs analyzed.
    /// </summary>
    public int TotalRuns { get; set; }

    /// <summary>
    /// The number of runs that had at least one grader pass.
    /// </summary>
    public int PassedRuns { get; set; }

    /// <summary>
    /// pass@k - Probability of at least 1 success in k runs.
    /// Formula: (runs with ≥1 passed grader) / total_runs
    /// Example: If 8 out of 10 runs had at least one success, pass@k = 0.8
    /// Use for tools needing one success (e.g., code generation).
    /// </summary>
    public float PassAtK { get; set; }

    /// <summary>
    /// pass^k - Probability that all k runs succeed.
    /// Formula: (runs with all graders passed) / total_runs
    /// Example: If only 3 out of 10 runs had all graders pass, pass^k = 0.3
    /// Use for customer-facing agents requiring consistency.
    /// </summary>
    public float PassToTheK { get; set; }

    /// <summary>
    /// The run IDs that were included in this metrics calculation.
    /// </summary>
    public IReadOnlyList<Guid> RunIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// The batch ID if these metrics are for a batch execution.
    /// </summary>
    public Guid? BatchId { get; set; }
}

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Service interface for executing AI tests.
/// Responsible for orchestrating test execution, grading, and result aggregation.
/// </summary>
public interface IAITestRunner
{
    /// <summary>
    /// Executes a test and returns the metrics.
    /// Creates N test runs (based on test.RunCount) and executes each independently.
    /// Grades each execution using configured graders and calculates pass@k metrics.
    /// </summary>
    /// <param name="test">The test to execute.</param>
    /// <param name="profileIdOverride">Optional profile ID to override the test's default profile.</param>
    /// <param name="contextIdsOverride">Optional context IDs to override for cross-model comparison.</param>
    /// <param name="batchId">Optional batch ID to group multiple runs together.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test metrics with pass@k calculations and run IDs.</returns>
    Task<AITestMetrics> ExecuteTestAsync(
        AITest test,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        Guid? batchId = null,
        CancellationToken cancellationToken = default);
}

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Service interface for executing AI tests.
/// Responsible for orchestrating test execution, grading, and result aggregation.
/// </summary>
public interface IAITestRunner
{
    /// <summary>
    /// Executes a test and returns the run result.
    /// Creates a new test run and executes the test N times (based on test.RunCount).
    /// Grades each execution using configured graders and calculates pass@k metrics.
    /// </summary>
    /// <param name="test">The test to execute.</param>
    /// <param name="profileIdOverride">Optional profile ID to override the test's default profile.</param>
    /// <param name="contextIdsOverride">Optional context IDs to override for cross-model comparison.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test run result with all transcripts and outcomes.</returns>
    Task<AITestRun> ExecuteTestAsync(
        AITest test,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default);
}

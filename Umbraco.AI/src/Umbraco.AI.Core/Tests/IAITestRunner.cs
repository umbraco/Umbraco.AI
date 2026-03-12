namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Service interface for executing AI tests.
/// Responsible for orchestrating test execution, grading, and result aggregation.
/// </summary>
public interface IAITestRunner
{
    /// <summary>
    /// Executes a test and returns per-variation execution results.
    /// Runs the default configuration plus all configured variations, grouped under a single execution ID.
    /// Grades each run using configured graders and calculates pass@k metrics per variation.
    /// </summary>
    /// <param name="test">The test to execute.</param>
    /// <param name="profileIdOverride">Optional profile ID to override the test's default profile (applies to default config only).</param>
    /// <param name="contextIdsOverride">Optional context IDs to override (applies to default config only).</param>
    /// <param name="batchId">Optional batch ID to group multiple test executions together.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result with per-variation metrics and aggregate metrics.</returns>
    Task<AITestExecutionResult> ExecuteTestAsync(
        AITest test,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        Guid? batchId = null,
        CancellationToken cancellationToken = default);
}

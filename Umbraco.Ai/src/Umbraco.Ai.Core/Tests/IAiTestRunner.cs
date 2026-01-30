namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Test execution orchestrator that runs tests, grades outcomes, and calculates metrics.
/// This is an internal service used by IAiTestService - it handles the execution layer.
/// </summary>
/// <remarks>
/// Following Anthropic's eval framework, the runner:
/// - Runs N executions per test (accounting for non-determinism)
/// - Ensures isolated environment per run (no shared state)
/// - Records structured transcripts for debugging
/// - Grades outcomes using configured graders
/// - Calculates pass@k (≥1 success) and pass^k (all succeed) metrics
/// - Supports profile and context overrides for comparison
/// </remarks>
public interface IAiTestRunner
{
    /// <summary>
    /// Runs a test with optional profile and context overrides.
    /// Executes N runs, grades each run, and calculates metrics.
    /// </summary>
    /// <param name="test">The test to run.</param>
    /// <param name="profileIdOverride">Optional profile override for cross-model comparison.</param>
    /// <param name="contextIdsOverride">Optional context IDs override for brand voice testing.</param>
    /// <param name="batchId">Optional batch ID to group multiple test executions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test metrics (pass@k, pass^k) and run IDs.</returns>
    Task<AiTestMetrics> RunTestAsync(
        AiTest test,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        Guid? batchId = null,
        CancellationToken cancellationToken = default);
}

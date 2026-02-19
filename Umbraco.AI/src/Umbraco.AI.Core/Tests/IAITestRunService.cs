using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Service interface for AI test run management and analysis.
/// Provides operations for querying, comparing, and managing test runs.
/// </summary>
public interface IAITestRunService
{
    /// <summary>
    /// Gets a test run by its unique identifier.
    /// </summary>
    /// <param name="id">The run ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test run if found, null otherwise.</returns>
    Task<AITestRun?> GetTestRunAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all runs for a specific test.
    /// </summary>
    /// <param name="testId">The test ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All runs for the test, ordered by execution date descending.</returns>
    Task<IEnumerable<AITestRun>> GetRunsByTestAsync(Guid testId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of runs with optional filtering.
    /// </summary>
    /// <param name="testId">Optional test ID to filter by.</param>
    /// <param name="batchId">Optional batch ID to filter by.</param>
    /// <param name="status">Optional status to filter by.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated runs and the total count.</returns>
    Task<(IEnumerable<AITestRun> Items, int Total)> GetRunsPagedAsync(
        Guid? testId = null,
        Guid? batchId = null,
        AITestRunStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent run for a test.
    /// </summary>
    /// <param name="testId">The test ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest run if found, null otherwise.</returns>
    Task<AITestRun?> GetLatestTestRunAsync(Guid testId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a run with its associated transcript.
    /// </summary>
    /// <param name="id">The run ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the run and transcript if found.</returns>
    Task<(AITestRun? Run, AITestTranscript? Transcript)> GetTestRunWithTranscriptAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two test runs and detects regressions.
    /// </summary>
    /// <param name="baselineTestRunId">The baseline test run ID.</param>
    /// <param name="comparisonTestRunId">The comparison test run ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison result with regression detection.</returns>
    Task<AITestRunComparison> CompareTestRunsAsync(
        Guid baselineTestRunId,
        Guid comparisonTestRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a run as the baseline for future comparisons.
    /// </summary>
    /// <param name="testId">The test ID.</param>
    /// <param name="testRunId">The test run ID to set as baseline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SetBaselineTestRunAsync(Guid testId, Guid testRunId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific test run.
    /// </summary>
    /// <param name="id">The run ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteTestRunAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old test runs based on retention policy.
    /// Keeps the last N runs per test.
    /// </summary>
    /// <param name="testId">The test ID.</param>
    /// <param name="keepCount">Number of recent runs to keep.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of runs deleted.</returns>
    Task<int> DeleteOldTestRunsAsync(Guid testId, int keepCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates metrics from a set of runs (batch or by test ID).
    /// </summary>
    /// <param name="runIds">The run IDs to calculate metrics from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test metrics with pass@k calculations.</returns>
    Task<AITestMetrics> CalculateMetricsAsync(
        IEnumerable<Guid> runIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Comparison result between two test runs.
/// </summary>
public sealed class AITestRunComparison
{
    /// <summary>
    /// The baseline run.
    /// </summary>
    public required AITestRun BaselineRun { get; set; }

    /// <summary>
    /// The comparison run.
    /// </summary>
    public required AITestRun ComparisonRun { get; set; }

    /// <summary>
    /// Whether the comparison run represents a regression from the baseline.
    /// True if comparison run failed where baseline passed.
    /// </summary>
    public bool IsRegression { get; set; }

    /// <summary>
    /// Whether the comparison run represents an improvement from the baseline.
    /// True if comparison run passed where baseline failed.
    /// </summary>
    public bool IsImprovement { get; set; }

    /// <summary>
    /// Change in duration (positive = slower, negative = faster).
    /// </summary>
    public long DurationChangeMs { get; set; }

    /// <summary>
    /// Grader-level comparison results.
    /// </summary>
    public IReadOnlyList<AITestGraderComparison> GraderComparisons { get; set; } = [];
}

/// <summary>
/// Comparison result for a specific grader between two runs.
/// </summary>
public sealed class AITestGraderComparison
{
    /// <summary>
    /// The grader ID.
    /// </summary>
    public required Guid GraderId { get; set; }

    /// <summary>
    /// The grader name.
    /// </summary>
    public required string GraderName { get; set; }

    /// <summary>
    /// Baseline grader result.
    /// </summary>
    public AITestGraderResult? BaselineResult { get; set; }

    /// <summary>
    /// Comparison grader result.
    /// </summary>
    public AITestGraderResult? ComparisonResult { get; set; }

    /// <summary>
    /// Whether this grader result changed between runs.
    /// </summary>
    public bool Changed { get; set; }

    /// <summary>
    /// Score change (positive = improvement, negative = regression).
    /// </summary>
    public double ScoreChange { get; set; }
}

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Service for managing AI tests and their execution.
/// Provides CRUD operations, test execution with profile/context overrides,
/// run management, and metrics calculation.
/// </summary>
public interface IAiTestService
{
    #region Test CRUD

    /// <summary>
    /// Gets a test by its ID.
    /// </summary>
    Task<AiTest?> GetTestAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a test by its alias (case-insensitive).
    /// </summary>
    Task<AiTest?> GetTestByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tests.
    /// </summary>
    Task<IEnumerable<AiTest>> GetAllTestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tests by tags (any match).
    /// </summary>
    Task<IEnumerable<AiTest>> GetTestsByTagsAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of tests with optional filtering.
    /// </summary>
    Task<(IEnumerable<AiTest> Items, int Total)> GetTestsPagedAsync(
        string? filter = null,
        string? testTypeId = null,
        bool? isEnabled = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a test alias exists (case-insensitive), optionally excluding a specific ID.
    /// </summary>
    Task<bool> TestAliasExistsAsync(
        string alias,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a test (creates or updates based on ID).
    /// </summary>
    Task<AiTest> SaveTestAsync(AiTest test, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a test by ID.
    /// </summary>
    Task DeleteTestAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Test Execution

    /// <summary>
    /// Runs a test with optional profile and context overrides.
    /// Executes N runs (based on test.RunCount), grades each run, and calculates metrics.
    /// </summary>
    /// <param name="testId">The test ID to run.</param>
    /// <param name="profileIdOverride">Optional profile override for cross-model comparison.</param>
    /// <param name="contextIdsOverride">Optional context IDs override for brand voice testing.</param>
    /// <param name="batchId">Optional batch ID to group multiple test executions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test metrics (pass@k, pass^k) and run IDs.</returns>
    Task<AiTestMetrics> RunTestAsync(
        Guid testId,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        Guid? batchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs multiple tests in a batch with optional overrides.
    /// </summary>
    Task<IDictionary<Guid, AiTestMetrics>> RunTestBatchAsync(
        IEnumerable<Guid> testIds,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs all tests matching the specified tags with optional overrides.
    /// </summary>
    Task<IDictionary<Guid, AiTestMetrics>> RunTestsByTagsAsync(
        IEnumerable<string> tags,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Run Management

    /// <summary>
    /// Gets a run by its ID.
    /// </summary>
    Task<AiTestRun?> GetRunAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a run by ID with transcript included.
    /// </summary>
    Task<AiTestRun?> GetRunWithTranscriptAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all runs for a specific test.
    /// </summary>
    Task<IEnumerable<AiTestRun>> GetRunsByTestAsync(
        Guid testId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest run for a specific test.
    /// </summary>
    Task<AiTestRun?> GetLatestRunAsync(Guid testId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of runs for a specific test.
    /// </summary>
    Task<(IEnumerable<AiTestRun> Items, int Total)> GetRunsPagedAsync(
        Guid testId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets runs by batch ID.
    /// </summary>
    Task<IEnumerable<AiTestRun>> GetRunsByBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a run as the baseline for regression detection.
    /// </summary>
    Task SetBaselineRunAsync(Guid testId, Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a run by ID.
    /// </summary>
    Task DeleteRunAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old runs for a test, keeping only the most recent N runs.
    /// </summary>
    Task DeleteOldRunsAsync(Guid testId, CancellationToken cancellationToken = default);

    #endregion

    #region Metrics

    /// <summary>
    /// Calculates metrics (pass@k, pass^k) for a set of runs.
    /// </summary>
    Task<AiTestMetrics> CalculateMetricsAsync(
        Guid testId,
        IEnumerable<Guid> runIds,
        CancellationToken cancellationToken = default);

    #endregion
}

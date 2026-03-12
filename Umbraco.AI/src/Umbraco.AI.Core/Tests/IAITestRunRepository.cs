namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Defines a repository for managing test run results.
/// Internal implementation detail - use <see cref="IAITestService"/> for external access.
/// </summary>
internal interface IAITestRunRepository
{
    /// <summary>
    /// Gets a run by its unique identifier.
    /// </summary>
    Task<AITestRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all runs for a test.
    /// </summary>
    Task<IEnumerable<AITestRun>> GetByTestIdAsync(Guid testId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets runs with pagination and optional filters.
    /// </summary>
    Task<(IEnumerable<AITestRun> Items, int Total)> GetPagedAsync(
        Guid? testId = null,
        Guid? batchId = null,
        AITestRunStatus? status = null,
        Guid? executionId = null,
        Guid? variationId = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all runs for a specific execution ID.
    /// </summary>
    Task<IEnumerable<AITestRun>> GetByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest run for a test.
    /// </summary>
    Task<AITestRun?> GetLatestByTestIdAsync(Guid testId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a run.
    /// </summary>
    Task<AITestRun> SaveAsync(AITestRun run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a run by its unique identifier.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all runs for a test except the last N runs.
    /// </summary>
    Task<int> DeleteOldRunsAsync(Guid testId, int keepCount, CancellationToken cancellationToken = default);
}

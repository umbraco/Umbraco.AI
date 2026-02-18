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
    /// Gets runs for a test with pagination.
    /// </summary>
    Task<(IEnumerable<AITestRun> Items, int Total)> GetPagedByTestIdAsync(
        Guid testId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets runs with pagination and optional filters.
    /// </summary>
    Task<Umbraco.Cms.Core.Models.PagedModel<AITestRun>> GetPagedAsync(
        Guid? testId = null,
        Guid? batchId = null,
        AITestRunStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest run for a test.
    /// </summary>
    Task<AITestRun?> GetLatestByTestIdAsync(Guid testId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all runs in a batch.
    /// </summary>
    Task<IEnumerable<AITestRun>> GetByBatchIdAsync(Guid batchId, CancellationToken cancellationToken = default);

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

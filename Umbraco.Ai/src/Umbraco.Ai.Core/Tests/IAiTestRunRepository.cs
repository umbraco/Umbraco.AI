namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Repository interface for AI test run persistence operations.
/// </summary>
public interface IAiTestRunRepository
{
    /// <summary>
    /// Gets a run by its ID.
    /// </summary>
    Task<AiTestRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a run by ID with transcript included.
    /// </summary>
    Task<AiTestRun?> GetByIdWithTranscriptAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all runs for a specific test.
    /// </summary>
    Task<IEnumerable<AiTestRun>> GetByTestIdAsync(Guid testId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest run for a specific test.
    /// </summary>
    Task<AiTestRun?> GetLatestByTestIdAsync(Guid testId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of runs for a specific test.
    /// </summary>
    Task<(IEnumerable<AiTestRun> Items, int Total)> GetPagedByTestIdAsync(
        Guid testId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets runs by batch ID.
    /// </summary>
    Task<IEnumerable<AiTestRun>> GetByBatchIdAsync(Guid batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new run.
    /// </summary>
    Task AddAsync(AiTestRun run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing run.
    /// </summary>
    Task UpdateAsync(AiTestRun run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a run by ID.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old runs for a test, keeping only the most recent N runs.
    /// </summary>
    Task DeleteOldRunsAsync(Guid testId, int keepCount, CancellationToken cancellationToken = default);
}

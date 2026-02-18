using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Service interface for AI test management operations.
/// </summary>
public interface IAITestService
{
    /// <summary>
    /// Gets a test by its unique identifier.
    /// </summary>
    /// <param name="id">The test ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test if found, null otherwise.</returns>
    Task<AITest?> GetTestAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a test by its alias.
    /// </summary>
    /// <param name="alias">The test alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test if found, null otherwise.</returns>
    Task<AITest?> GetTestByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tests.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All tests.</returns>
    Task<IEnumerable<AITest>> GetTestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of tests with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="tags">Optional tags filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing tests and total count.</returns>
    Task<PagedModel<AITest>> GetTestsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a test (insert if new, update if exists) with validation.
    /// If test.Id is Guid.Empty, a new Guid will be generated.
    /// </summary>
    /// <param name="test">The test to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved test.</returns>
    Task<AITest> SaveTestAsync(AITest test, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a test and all its runs.
    /// </summary>
    /// <param name="id">The test ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteTestAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a test with the given alias exists.
    /// </summary>
    /// <param name="alias">The test alias.</param>
    /// <param name="excludeId">Optional ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alias exists.</returns>
    Task<bool> TestAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a test and returns the metrics.
    /// Creates N test runs (based on test.RunCount) and calculates pass@k metrics.
    /// </summary>
    /// <param name="testId">The test ID to execute.</param>
    /// <param name="profileIdOverride">Optional profile ID to override the test's default profile.</param>
    /// <param name="contextIdsOverride">Optional context IDs to override for cross-model comparison.</param>
    /// <param name="batchId">Optional batch ID to group multiple runs together.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test metrics with pass@k calculations and run IDs.</returns>
    Task<AITestMetrics> RunTestAsync(
        Guid testId,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        Guid? batchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple tests in batch and returns metrics for each.
    /// All tests in the batch share the same batch ID for grouping.
    /// </summary>
    /// <param name="testIds">The test IDs to execute.</param>
    /// <param name="profileIdOverride">Optional profile ID to override for all tests.</param>
    /// <param name="contextIdsOverride">Optional context IDs to override for all tests.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of test ID to metrics.</returns>
    Task<IDictionary<Guid, AITestMetrics>> RunTestBatchAsync(
        IEnumerable<Guid> testIds,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all tests with the specified tags and returns metrics for each.
    /// All tests in the batch share the same batch ID for grouping.
    /// </summary>
    /// <param name="tags">The tags to filter tests by. Tests must have ALL specified tags.</param>
    /// <param name="profileIdOverride">Optional profile ID to override for all tests.</param>
    /// <param name="contextIdsOverride">Optional context IDs to override for all tests.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of test ID to metrics.</returns>
    Task<IDictionary<Guid, AITestMetrics>> RunTestsByTagsAsync(
        IEnumerable<string> tags,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default);
}

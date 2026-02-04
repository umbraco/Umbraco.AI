namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Defines a repository for managing AI tests.
/// Internal implementation detail - use <see cref="IAITestService"/> for external access.
/// </summary>
internal interface IAITestRepository
{
    /// <summary>
    /// Gets a test by its unique identifier.
    /// </summary>
    Task<AITest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a test by its alias.
    /// </summary>
    Task<AITest?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tests.
    /// </summary>
    Task<IEnumerable<AITest>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tests by tag.
    /// </summary>
    Task<IEnumerable<AITest>> GetByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tests with pagination and optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter to search by name (case-insensitive contains).</param>
    /// <param name="testTypeId">Optional test type to filter by.</param>
    /// <param name="isEnabled">Optional enabled status filter.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated tests and the total count.</returns>
    Task<(IEnumerable<AITest> Items, int Total)> GetPagedAsync(
        string? filter = null,
        string? testTypeId = null,
        bool? isEnabled = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a test.
    /// </summary>
    /// <param name="test">The test to save.</param>
    /// <param name="userId">Optional user key (GUID) for version tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved test.</returns>
    Task<AITest> SaveAsync(AITest test, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a test by its unique identifier.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

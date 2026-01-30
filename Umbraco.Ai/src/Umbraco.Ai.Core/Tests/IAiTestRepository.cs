namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Repository interface for AI test persistence operations.
/// </summary>
public interface IAiTestRepository
{
    /// <summary>
    /// Gets a test by its ID.
    /// </summary>
    Task<AiTest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a test by its alias (case-insensitive).
    /// </summary>
    Task<AiTest?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tests.
    /// </summary>
    Task<IEnumerable<AiTest>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tests by tags (any match).
    /// </summary>
    Task<IEnumerable<AiTest>> GetByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of tests with optional filtering.
    /// </summary>
    Task<(IEnumerable<AiTest> Items, int Total)> GetPagedAsync(
        string? filter = null,
        string? testTypeId = null,
        bool? isEnabled = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a test alias exists (case-insensitive), optionally excluding a specific ID.
    /// </summary>
    Task<bool> AliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new test.
    /// </summary>
    Task AddAsync(AiTest test, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing test.
    /// </summary>
    Task UpdateAsync(AiTest test, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a test by ID.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Service implementation for AI test management operations.
/// </summary>
internal sealed class AITestService : IAITestService
{
    private readonly IAITestRepository _repository;
    private readonly IAIEntityVersionService _versionService;
    private readonly IAITestRunner _testRunner;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;

    public AITestService(
        IAITestRepository repository,
        IAIEntityVersionService versionService,
        IAITestRunner testRunner,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _versionService = versionService;
        _testRunner = testRunner;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public Task<AITest?> GetTestAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AITest?> GetTestByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AITest>> GetTestsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<PagedModel<AITest>> GetTestsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _repository.GetPagedAsync(filter, null, null, skip, take, cancellationToken);
        return new PagedModel<AITest>(total, items);
    }

    /// <inheritdoc />
    public async Task<AITest> SaveTestAsync(AITest test, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentException.ThrowIfNullOrWhiteSpace(test.Alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(test.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(test.TestTypeId);

        // Validate test case JSON is not empty
        if (string.IsNullOrWhiteSpace(test.TestCase.TestCaseJson))
        {
            throw new InvalidOperationException("Test case JSON cannot be empty");
        }

        // Validate target
        ArgumentException.ThrowIfNullOrWhiteSpace(test.Target.TargetId);

        // Validate run count
        if (test.RunCount < 1)
        {
            throw new InvalidOperationException("Run count must be at least 1");
        }

        // Generate new ID if needed
        if (test.Id == Guid.Empty)
        {
            test.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(test.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != test.Id)
        {
            throw new InvalidOperationException($"A test with alias '{test.Alias}' already exists.");
        }

        // Update timestamp
        test.DateModified = DateTime.UtcNow;

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Save version snapshot of existing entity before update
        var existing = await _repository.GetByIdAsync(test.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        return await _repository.SaveAsync(test, userId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteTestAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> TestAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var existingTest = await _repository.GetByAliasAsync(alias, cancellationToken);
        return existingTest != null && existingTest.Id != excludeId;
    }

    /// <inheritdoc />
    public async Task<AITestRun> RunTestAsync(
        Guid testId,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default)
    {
        // Get the test
        var test = await GetTestAsync(testId, cancellationToken)
            ?? throw new InvalidOperationException($"Test {testId} not found");

        // Delegate to test runner
        return await _testRunner.ExecuteTestAsync(test, profileIdOverride, contextIdsOverride, cancellationToken);
    }
}

using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Service implementation for AI test management operations.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Note: TestRunner vs EventAggregator</strong>
/// </para>
/// <para>
/// Unlike <see cref="Profiles.AIProfileService"/> and <see cref="Connections.AIConnectionService"/>,
/// this service uses <see cref="IAITestRunner"/> as a core dependency instead of relying primarily
/// on <see cref="IEventAggregator"/> for extensibility.
/// </para>
/// <para>
/// <strong>Rationale:</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Tests are <strong>execution-focused</strong> - The primary purpose is running tests and
/// collecting metrics, not just configuration management like Profiles/Connections.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>EventAggregator is still present</strong> - Notifications (Saving, Saved, Deleting,
/// Deleted, RollingBack, RolledBack) are published for CRUD operations to maintain consistency
/// with other core entities and allow external extensibility.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>TestRunner handles execution complexity</strong> - Test execution involves running
/// multiple iterations, managing test harnesses, applying graders, calculating pass@k metrics,
/// and storing transcripts. This domain logic belongs in a dedicated runner service.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Separation of concerns</strong> - CRUD operations (save/delete) use event notifications
/// for extensibility, while execution operations use the runner for domain logic. This keeps
/// each concern focused and testable.
/// </description>
/// </item>
/// </list>
/// </remarks>
internal sealed class AITestService : IAITestService
{
    private readonly IAITestRepository _repository;
    private readonly IAIEntityVersionService _versionService;
    private readonly IAITestRunner _testRunner;
    private readonly IEventAggregator _eventAggregator;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;

    public AITestService(
        IAITestRepository repository,
        IAIEntityVersionService versionService,
        IAITestRunner testRunner,
        IEventAggregator eventAggregator,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _versionService = versionService;
        _testRunner = testRunner;
        _eventAggregator = eventAggregator;
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
    public Task<(IEnumerable<AITest> Items, int Total)> GetTestsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(filter, null, null, skip, take, cancellationToken);

    /// <inheritdoc />
    public async Task<AITest> SaveTestAsync(AITest test, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentException.ThrowIfNullOrWhiteSpace(test.Alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(test.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(test.TestFeatureId);

        // Validate test case is not null
        if (test.TestCase == null)
        {
            throw new InvalidOperationException("Test case cannot be null");
        }

        // Validate target ID
        if (test.TestTargetId == Guid.Empty)
        {
            throw new InvalidOperationException("Test target ID cannot be empty");
        }

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

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Publish saving notification (before save)
        var messages = new EventMessages();
        var savingNotification = new AITestSavingNotification(test, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);

        // Check if cancelled
        if (savingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Test save cancelled: {errorMessages}");
        }

        // Update timestamp
        test.DateModified = DateTime.UtcNow;

        // Save version snapshot of existing entity before update
        var existing = await _repository.GetByIdAsync(test.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        // Perform save
        var savedTest = await _repository.SaveAsync(test, userId, cancellationToken);

        // Publish saved notification (after save)
        var savedNotification = new AITestSavedNotification(savedTest, messages);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return savedTest;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Publish deleting notification (before delete)
        var messages = new EventMessages();
        var deletingNotification = new AITestDeletingNotification(id, messages);
        await _eventAggregator.PublishAsync(deletingNotification, cancellationToken);

        // Check if cancelled
        if (deletingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Test delete cancelled: {errorMessages}");
        }

        // Delete version history for this entity
        await _versionService.DeleteVersionsAsync(id, "Test", cancellationToken);

        // Perform delete
        var result = await _repository.DeleteAsync(id, cancellationToken);

        // Publish deleted notification (after delete)
        var deletedNotification = new AITestDeletedNotification(id, messages);
        await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> TestExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var test = await _repository.GetByIdAsync(id, cancellationToken);
        return test is not null;
    }

    /// <inheritdoc />
    public async Task<bool> TestAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var existingTest = await _repository.GetByAliasAsync(alias, cancellationToken);
        return existingTest != null && existingTest.Id != excludeId;
    }

    /// <inheritdoc />
    public async Task<AITest> RollbackTestAsync(
        Guid testId,
        int targetVersion,
        CancellationToken cancellationToken = default)
    {
        // Get the current test to ensure it exists
        var currentTest = await _repository.GetByIdAsync(testId, cancellationToken);
        if (currentTest is null)
        {
            throw new InvalidOperationException($"Test with ID '{testId}' not found.");
        }

        // Get the snapshot at the target version
        var snapshot = await _versionService.GetVersionSnapshotAsync<AITest>(testId, targetVersion, cancellationToken);
        if (snapshot is null)
        {
            throw new InvalidOperationException($"Version {targetVersion} not found for test '{testId}'.");
        }

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Publish rolling back notification (before rollback)
        var messages = new EventMessages();
        var rollingBackNotification = new AITestRollingBackNotification(testId, targetVersion, messages);
        await _eventAggregator.PublishAsync(rollingBackNotification, cancellationToken);

        // Check if cancelled
        if (rollingBackNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Test rollback cancelled: {errorMessages}");
        }

        // Save the current state to version history before rolling back
        await _versionService.SaveVersionAsync(currentTest, userId, null, cancellationToken);

        // Create a new version by saving the snapshot data
        // We need to preserve the ID and update the dates appropriately
        var rolledBackTest = new AITest
        {
            Id = testId,
            Alias = snapshot.Alias,
            Name = snapshot.Name,
            Description = snapshot.Description,
            TestFeatureId = snapshot.TestFeatureId,
            TestTargetId = snapshot.TestTargetId,
            TestCase = snapshot.TestCase,
            Graders = snapshot.Graders,
            RunCount = snapshot.RunCount,
            Tags = snapshot.Tags,
            IsActive = snapshot.IsActive,
            BaselineRunId = snapshot.BaselineRunId,
            DateCreated = snapshot.DateCreated,
            DateModified = DateTime.UtcNow
        };

        // Save without versioning (we already versioned the current state above)
        var savedTest = await _repository.SaveAsync(rolledBackTest, userId, cancellationToken);

        // Publish rolled back notification (after rollback)
        var rolledBackNotification = new AITestRolledBackNotification(testId, targetVersion, messages);
        await _eventAggregator.PublishAsync(rolledBackNotification, cancellationToken);

        return savedTest;
    }

    /// <inheritdoc />
    public async Task<AITestMetrics> RunTestAsync(
        Guid testId,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        Guid? batchId = null,
        CancellationToken cancellationToken = default)
    {
        // Get the test
        var test = await GetTestAsync(testId, cancellationToken)
            ?? throw new InvalidOperationException($"Test {testId} not found");

        // Delegate to test runner
        return await _testRunner.ExecuteTestAsync(test, profileIdOverride, contextIdsOverride, batchId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IDictionary<Guid, AITestMetrics>> RunTestBatchAsync(
        IEnumerable<Guid> testIds,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default)
    {
        var testIdsList = testIds.ToList();
        if (testIdsList.Count == 0)
        {
            return new Dictionary<Guid, AITestMetrics>();
        }

        // Generate a batch ID for all tests in this batch
        var batchId = Guid.NewGuid();

        // Execute all tests with the same batch ID
        var results = new Dictionary<Guid, AITestMetrics>();
        foreach (var testId in testIdsList)
        {
            try
            {
                var metrics = await RunTestAsync(testId, profileIdOverride, contextIdsOverride, batchId, cancellationToken);
                results[testId] = metrics;
            }
            catch (InvalidOperationException)
            {
                // Test not found - skip it
                continue;
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IDictionary<Guid, AITestMetrics>> RunTestsByTagsAsync(
        IEnumerable<string> tags,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default)
    {
        var tagsList = tags.ToList();
        if (tagsList.Count == 0)
        {
            return new Dictionary<Guid, AITestMetrics>();
        }

        // Get all tests
        var allTests = await GetTestsAsync(cancellationToken);

        // Filter tests that have ALL specified tags
        var matchingTests = allTests.Where(test =>
            tagsList.All(tag => test.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));

        // Execute the matching tests as a batch
        var testIds = matchingTests.Select(t => t.Id);
        return await RunTestBatchAsync(testIds, profileIdOverride, contextIdsOverride, cancellationToken);
    }
}

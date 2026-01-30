using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Service for managing AI tests and their execution.
/// </summary>
internal class AiTestService : IAiTestService
{
    private readonly IAiTestRepository _testRepository;
    private readonly IAiTestRunRepository _runRepository;
    private readonly IAiTestRunner _testRunner;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;
    private readonly AiOptions _options;

    public AiTestService(
        IAiTestRepository testRepository,
        IAiTestRunRepository runRepository,
        IAiTestRunner testRunner,
        IBackOfficeSecurityAccessor securityAccessor,
        IOptions<AiOptions> options)
    {
        _testRepository = testRepository;
        _runRepository = runRepository;
        _testRunner = testRunner;
        _securityAccessor = securityAccessor;
        _options = options.Value;
    }

    #region Test CRUD

    /// <inheritdoc />
    public async Task<AiTest?> GetTestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _testRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiTest?> GetTestByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        return await _testRepository.GetByAliasAsync(alias, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiTest>> GetAllTestsAsync(CancellationToken cancellationToken = default)
    {
        return await _testRepository.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiTest>> GetTestsByTagsAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        return await _testRepository.GetByTagsAsync(tags, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AiTest> Items, int Total)> GetTestsPagedAsync(
        string? filter = null,
        string? testTypeId = null,
        bool? isEnabled = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _testRepository.GetPagedAsync(filter, testTypeId, isEnabled, skip, take, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TestAliasExistsAsync(
        string alias,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        return await _testRepository.AliasExistsAsync(alias, excludeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiTest> SaveTestAsync(AiTest test, CancellationToken cancellationToken = default)
    {
        var currentUserId = _securityAccessor.BackOfficeSecurity?.CurrentUser?.Key;

        if (test.Id == Guid.Empty)
        {
            // Create new test
            test.GetType().GetProperty(nameof(AiTest.DateCreated))!
                .SetValue(test, DateTime.UtcNow);
            test.GetType().GetProperty(nameof(AiTest.CreatedByUserId))!
                .SetValue(test, currentUserId);
            test.GetType().GetProperty(nameof(AiTest.DateModified))!
                .SetValue(test, DateTime.UtcNow);
            test.ModifiedByUserId = currentUserId;

            await _testRepository.AddAsync(test, cancellationToken);
        }
        else
        {
            // Update existing test
            test.GetType().GetProperty(nameof(AiTest.DateModified))!
                .SetValue(test, DateTime.UtcNow);
            test.ModifiedByUserId = currentUserId;

            // Increment version
            test.GetType().GetProperty(nameof(AiTest.Version))!
                .SetValue(test, test.Version + 1);

            await _testRepository.UpdateAsync(test, cancellationToken);
        }

        return test;
    }

    /// <inheritdoc />
    public async Task DeleteTestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _testRepository.DeleteAsync(id, cancellationToken);
    }

    #endregion

    #region Test Execution

    /// <inheritdoc />
    public async Task<AiTestMetrics> RunTestAsync(
        Guid testId,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        Guid? batchId = null,
        CancellationToken cancellationToken = default)
    {
        var test = await _testRepository.GetByIdAsync(testId, cancellationToken)
            ?? throw new InvalidOperationException($"Test with ID {testId} not found");

        // Delegate to test runner
        var metrics = await _testRunner.RunTestAsync(test, profileIdOverride, contextIdsOverride, batchId, cancellationToken);

        // Apply retention policy after execution
        if (_options.Test.RunRetentionCount > 0)
        {
            await _runRepository.DeleteOldRunsAsync(testId, _options.Test.RunRetentionCount, cancellationToken);
        }

        return metrics;
    }

    /// <inheritdoc />
    public async Task<IDictionary<Guid, AiTestMetrics>> RunTestBatchAsync(
        IEnumerable<Guid> testIds,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default)
    {
        var batchId = Guid.NewGuid();
        var results = new Dictionary<Guid, AiTestMetrics>();

        foreach (var testId in testIds)
        {
            var metrics = await RunTestAsync(testId, profileIdOverride, contextIdsOverride, batchId, cancellationToken);
            results[testId] = metrics;
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IDictionary<Guid, AiTestMetrics>> RunTestsByTagsAsync(
        IEnumerable<string> tags,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default)
    {
        var tests = await _testRepository.GetByTagsAsync(tags, cancellationToken);
        var testIds = tests.Where(t => t.IsEnabled).Select(t => t.Id);

        return await RunTestBatchAsync(testIds, profileIdOverride, contextIdsOverride, cancellationToken);
    }

    #endregion

    #region Run Management

    /// <inheritdoc />
    public async Task<AiTestRun?> GetRunAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _runRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiTestRun?> GetRunWithTranscriptAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _runRepository.GetByIdWithTranscriptAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiTestRun>> GetRunsByTestAsync(
        Guid testId,
        CancellationToken cancellationToken = default)
    {
        return await _runRepository.GetByTestIdAsync(testId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiTestRun?> GetLatestRunAsync(Guid testId, CancellationToken cancellationToken = default)
    {
        return await _runRepository.GetLatestByTestIdAsync(testId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AiTestRun> Items, int Total)> GetRunsPagedAsync(
        Guid testId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _runRepository.GetPagedByTestIdAsync(testId, skip, take, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiTestRun>> GetRunsByBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        return await _runRepository.GetByBatchIdAsync(batchId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetBaselineRunAsync(Guid testId, Guid runId, CancellationToken cancellationToken = default)
    {
        var test = await _testRepository.GetByIdAsync(testId, cancellationToken)
            ?? throw new InvalidOperationException($"Test with ID {testId} not found");

        test.GetType().GetProperty(nameof(AiTest.BaselineRunId))!
            .SetValue(test, runId);

        await _testRepository.UpdateAsync(test, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteRunAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _runRepository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteOldRunsAsync(Guid testId, CancellationToken cancellationToken = default)
    {
        if (_options.Test.RunRetentionCount > 0)
        {
            await _runRepository.DeleteOldRunsAsync(testId, _options.Test.RunRetentionCount, cancellationToken);
        }
    }

    #endregion

    #region Metrics

    /// <inheritdoc />
    public async Task<AiTestMetrics> CalculateMetricsAsync(
        Guid testId,
        IEnumerable<Guid> runIds,
        CancellationToken cancellationToken = default)
    {
        var runs = new List<AiTestRun>();

        foreach (var runId in runIds)
        {
            var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
            if (run != null)
            {
                runs.Add(run);
            }
        }

        return CalculateMetrics(testId, runs);
    }

    private static AiTestMetrics CalculateMetrics(Guid testId, IEnumerable<AiTestRun> runs)
    {
        var runsList = runs.ToList();
        var totalRuns = runsList.Count;

        if (totalRuns == 0)
        {
            return new AiTestMetrics
            {
                TestId = testId,
                TotalRuns = 0,
                PassedRuns = 0,
                PassAtK = 0,
                PassToTheK = 0,
                RunIds = Array.Empty<Guid>()
            };
        }

        // pass@k: Runs where at least one grader passed (or no graders)
        var passedRuns = runsList.Count(r =>
            r.Status == AiTestRunStatus.Passed ||
            (r.GraderResults.Count > 0 && r.GraderResults.Any(g => g.Passed)));

        // pass^k: Runs where ALL graders passed (or no graders)
        var allPassedRuns = runsList.Count(r =>
            r.Status == AiTestRunStatus.Passed ||
            (r.GraderResults.Count > 0 && r.GraderResults.All(g => g.Passed)));

        return new AiTestMetrics
        {
            TestId = testId,
            TotalRuns = totalRuns,
            PassedRuns = passedRuns,
            PassAtK = (float)passedRuns / totalRuns,
            PassToTheK = (float)allPassedRuns / totalRuns,
            RunIds = runsList.Select(r => r.Id).ToArray()
        };
    }

    #endregion
}

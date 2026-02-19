using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Service implementation for AI test run management and analysis.
/// </summary>
internal sealed class AITestRunService : IAITestRunService
{
    private readonly IAITestRunRepository _runRepository;
    private readonly IAITestTranscriptRepository _transcriptRepository;
    private readonly IAITestRepository _testRepository;

    public AITestRunService(
        IAITestRunRepository runRepository,
        IAITestTranscriptRepository transcriptRepository,
        IAITestRepository testRepository)
    {
        _runRepository = runRepository;
        _transcriptRepository = transcriptRepository;
        _testRepository = testRepository;
    }

    /// <inheritdoc />
    public Task<AITestRun?> GetRunAsync(Guid id, CancellationToken cancellationToken = default)
        => _runRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AITestRun>> GetRunsByTestAsync(Guid testId, CancellationToken cancellationToken = default)
    {
        // Repository already returns runs ordered by ExecutedAt descending
        return _runRepository.GetByTestIdAsync(testId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<(IEnumerable<AITestRun> Items, int Total)> GetRunsPagedAsync(
        Guid? testId = null,
        Guid? batchId = null,
        AITestRunStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
        => _runRepository.GetPagedAsync(testId, batchId, status, skip, take, cancellationToken);

    /// <inheritdoc />
    public Task<AITestRun?> GetLatestRunAsync(Guid testId, CancellationToken cancellationToken = default)
    {
        // Use efficient repository method that fetches only the latest run at database level
        return _runRepository.GetLatestByTestIdAsync(testId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(AITestRun? Run, AITestTranscript? Transcript)> GetRunWithTranscriptAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var run = await _runRepository.GetByIdAsync(id, cancellationToken);
        if (run is null)
        {
            return (null, null);
        }

        AITestTranscript? transcript = null;
        if (run.TranscriptId.HasValue)
        {
            transcript = await _transcriptRepository.GetByIdAsync(run.TranscriptId.Value, cancellationToken);
        }

        return (run, transcript);
    }

    /// <inheritdoc />
    public async Task<AITestRunComparison> CompareRunsAsync(
        Guid baselineRunId,
        Guid comparisonRunId,
        CancellationToken cancellationToken = default)
    {
        var baselineRun = await _runRepository.GetByIdAsync(baselineRunId, cancellationToken)
            ?? throw new InvalidOperationException($"Baseline run {baselineRunId} not found");

        var comparisonRun = await _runRepository.GetByIdAsync(comparisonRunId, cancellationToken)
            ?? throw new InvalidOperationException($"Comparison run {comparisonRunId} not found");

        // Detect regression/improvement
        var isRegression = baselineRun.Status == AITestRunStatus.Passed && comparisonRun.Status != AITestRunStatus.Passed;
        var isImprovement = baselineRun.Status != AITestRunStatus.Passed && comparisonRun.Status == AITestRunStatus.Passed;

        // Calculate duration change
        var durationChange = comparisonRun.DurationMs - baselineRun.DurationMs;

        // Compare grader results
        var graderComparisons = new List<AITestGraderComparison>();

        // Get all unique grader IDs from both runs
        var allGraderIds = baselineRun.GraderResults
            .Select(r => r.GraderId)
            .Union(comparisonRun.GraderResults.Select(r => r.GraderId))
            .Distinct();

        foreach (var graderId in allGraderIds)
        {
            var baselineResult = baselineRun.GraderResults.FirstOrDefault(r => r.GraderId == graderId);
            var comparisonResult = comparisonRun.GraderResults.FirstOrDefault(r => r.GraderId == graderId);

            var changed = false;
            var scoreChange = 0.0;

            if (baselineResult != null && comparisonResult != null)
            {
                changed = baselineResult.Passed != comparisonResult.Passed ||
                         Math.Abs(baselineResult.Score - comparisonResult.Score) > 0.001;
                scoreChange = comparisonResult.Score - baselineResult.Score;
            }
            else
            {
                // Grader was added or removed between runs
                changed = true;
            }

            graderComparisons.Add(new AITestGraderComparison
            {
                GraderId = graderId,
                GraderName = baselineResult?.ToString() ?? comparisonResult?.ToString() ?? "Unknown",
                BaselineResult = baselineResult,
                ComparisonResult = comparisonResult,
                Changed = changed,
                ScoreChange = scoreChange
            });
        }

        return new AITestRunComparison
        {
            BaselineRun = baselineRun,
            ComparisonRun = comparisonRun,
            IsRegression = isRegression,
            IsImprovement = isImprovement,
            DurationChangeMs = durationChange,
            GraderComparisons = graderComparisons
        };
    }

    /// <inheritdoc />
    public async Task<bool> SetBaselineRunAsync(Guid testId, Guid runId, CancellationToken cancellationToken = default)
    {
        // Verify the run exists and belongs to the test
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null || run.TestId != testId)
        {
            return false;
        }

        // Verify the test exists
        var test = await _testRepository.GetByIdAsync(testId, cancellationToken);
        if (test is null)
        {
            return false;
        }

        // Update the test's baseline run ID
        test.BaselineRunId = runId;
        await _testRepository.SaveAsync(test, userId: null, cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRunAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var run = await _runRepository.GetByIdAsync(id, cancellationToken);
        if (run is null)
        {
            return false;
        }

        // Delete associated transcript if exists
        if (run.TranscriptId.HasValue)
        {
            await _transcriptRepository.DeleteAsync(run.TranscriptId.Value, cancellationToken);
        }

        // Delete the run
        return await _runRepository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> DeleteOldRunsAsync(Guid testId, int keepCount, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keepCount);

        // Use efficient repository method that performs bulk deletion with transcript cleanup
        return _runRepository.DeleteOldRunsAsync(testId, keepCount, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AITestMetrics> CalculateMetricsAsync(
        IEnumerable<Guid> runIds,
        CancellationToken cancellationToken = default)
    {
        var runIdsList = runIds.ToList();
        if (runIdsList.Count == 0)
        {
            throw new ArgumentException("At least one run ID must be provided", nameof(runIds));
        }

        // Fetch all runs
        var runs = new List<AITestRun>();
        foreach (var runId in runIdsList)
        {
            var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
            if (run != null)
            {
                runs.Add(run);
            }
        }

        if (runs.Count == 0)
        {
            throw new InvalidOperationException("No valid runs found for the provided IDs");
        }

        // All runs should be for the same test
        var testId = runs[0].TestId;
        if (runs.Any(r => r.TestId != testId))
        {
            throw new InvalidOperationException("All runs must belong to the same test");
        }

        // Calculate metrics
        var totalRuns = runs.Count;
        var passedRuns = runs.Count(r => r.Status == AITestRunStatus.Passed);

        var passAtK = totalRuns > 0 ? (double)passedRuns / totalRuns : 0.0;
        var passToTheK = passedRuns == totalRuns && totalRuns > 0 ? 1.0 : 0.0;

        return new AITestMetrics
        {
            TestId = testId,
            TotalRuns = totalRuns,
            PassedRuns = passedRuns,
            PassAtK = passAtK,
            PassToTheK = passToTheK,
            RunIds = runs.Select(r => r.Id).ToList()
        };
    }
}

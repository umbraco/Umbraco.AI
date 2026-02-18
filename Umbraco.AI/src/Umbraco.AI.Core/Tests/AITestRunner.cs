using System.Text.Json;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Service implementation for executing AI tests.
/// Orchestrates test execution, grading, and result aggregation.
/// </summary>
internal sealed class AITestRunner : IAITestRunner
{
    private readonly IAITestRunRepository _runRepository;
    private readonly AITestFeatureCollection _testFeatures;
    private readonly AITestGraderCollection _testGraders;

    public AITestRunner(
        IAITestRunRepository runRepository,
        AITestFeatureCollection testFeatures,
        AITestGraderCollection testGraders)
    {
        _runRepository = runRepository;
        _testFeatures = testFeatures;
        _testGraders = testGraders;
    }

    /// <inheritdoc />
    public async Task<AITestMetrics> ExecuteTestAsync(
        AITest test,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        Guid? batchId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(test);

        // Get the test feature
        var testFeature = _testFeatures.FirstOrDefault(f => f.Id == test.TestTypeId)
            ?? throw new InvalidOperationException($"Test feature '{test.TestTypeId}' not found");

        // Generate batch ID if not provided
        var effectiveBatchId = batchId ?? Guid.NewGuid();

        // Execute N runs (test.RunCount)
        var runs = new List<AITestRun>();
        for (int runNumber = 1; runNumber <= test.RunCount; runNumber++)
        {
            var run = await ExecuteSingleRunAsync(
                test,
                testFeature,
                runNumber,
                profileIdOverride,
                contextIdsOverride,
                effectiveBatchId,
                cancellationToken);

            runs.Add(run);

            // Save each run immediately
            await _runRepository.SaveAsync(run, cancellationToken);
        }

        // Calculate metrics
        return CalculateMetrics(test.Id, runs);
    }

    /// <summary>
    /// Executes a single test run.
    /// </summary>
    private async Task<AITestRun> ExecuteSingleRunAsync(
        AITest test,
        IAITestFeature testFeature,
        int runNumber,
        Guid? profileIdOverride,
        IEnumerable<Guid>? contextIdsOverride,
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        // Create test run
        var testRun = new AITestRun
        {
            Id = Guid.NewGuid(),
            TestId = test.Id,
            TestVersion = test.Version,
            RunNumber = runNumber,
            ProfileId = profileIdOverride,
            ContextIds = contextIdsOverride?.ToList() ?? new List<Guid>(),
            ExecutedAt = startTime,
            Status = AITestRunStatus.Running,
            BatchId = batchId
        };

        try
        {
            // Execute the test feature
            var transcript = await testFeature.ExecuteAsync(
                test,
                runNumber,
                testRun.ProfileId,
                testRun.ContextIds,
                cancellationToken);

            // Set run ID and save transcript
            transcript.RunId = testRun.Id;
            testRun.TranscriptId = transcript.Id;

            // Build outcome from transcript
            var outcome = new AITestOutcome
            {
                OutputType = AITestOutputType.Text, // TODO: Detect from transcript
                OutputValue = transcript.FinalOutputJson,
                FinishReason = "completed", // TODO: Get from transcript
                TokenUsageJson = null // TODO: Extract from transcript timing/metadata if available
            };

            // Store outcome
            testRun.Outcome = outcome;

            // Grade the outcome using configured graders
            var graderResults = await GradeOutcomeAsync(test, transcript, outcome, cancellationToken);
            testRun.GraderResults = graderResults;

            // Calculate aggregate pass/fail (only Error severity counts toward pass/fail)
            var errorGraders = graderResults.Where(r => r.Severity == AITestGraderSeverity.Error).ToList();
            var passed = errorGraders.Count == 0 || errorGraders.All(r => r.Passed);

            // Set final status
            testRun.Status = passed ? AITestRunStatus.Passed : AITestRunStatus.Failed;
            testRun.DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            testRun.Status = AITestRunStatus.Error;
            testRun.DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            testRun.MetadataJson = JsonSerializer.Serialize(new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }

        return testRun;
    }

    /// <summary>
    /// Grades a test outcome using all configured graders.
    /// </summary>
    private async Task<List<AITestGraderResult>> GradeOutcomeAsync(
        AITest test,
        AITestTranscript transcript,
        AITestOutcome outcome,
        CancellationToken cancellationToken)
    {
        var graderResults = new List<AITestGraderResult>();

        foreach (var grader in test.Graders)
        {
            var graderImpl = _testGraders.FirstOrDefault(g => g.Id == grader.GraderTypeId);
            if (graderImpl == null)
            {
                // Grader not found - record as error
                graderResults.Add(new AITestGraderResult
                {
                    GraderId = grader.Id,
                    Passed = false,
                    Score = 0.0,
                    FailureMessage = $"Grader implementation '{grader.GraderTypeId}' not found",
                    Severity = grader.Severity
                });
                continue;
            }

            try
            {
                var result = await graderImpl.GradeAsync(transcript, outcome, grader, cancellationToken);

                // Apply negation if configured
                if (grader.Negate)
                {
                    result.Passed = !result.Passed;
                    result.Score = 1.0 - result.Score;
                    result.FailureMessage = result.Passed
                        ? null
                        : $"Negated: {result.FailureMessage ?? "Expected to fail but passed"}";
                }

                // Apply severity
                result.Severity = grader.Severity;

                graderResults.Add(result);
            }
            catch (Exception ex)
            {
                // Grader execution failed - record as error
                graderResults.Add(new AITestGraderResult
                {
                    GraderId = grader.Id,
                    Passed = false,
                    Score = 0.0,
                    FailureMessage = $"Grader execution failed: {ex.Message}",
                    Severity = grader.Severity
                });
            }
        }

        return graderResults;
    }

    /// <summary>
    /// Calculates pass@k and pass^k metrics from a set of runs.
    /// </summary>
    private static AITestMetrics CalculateMetrics(Guid testId, IReadOnlyList<AITestRun> runs)
    {
        var totalRuns = runs.Count;
        var passedRuns = runs.Count(r => r.Status == AITestRunStatus.Passed);

        // pass@k = probability that at least one run succeeds
        // Formula: PassedRuns / TotalRuns
        var passAtK = totalRuns > 0 ? (double)passedRuns / totalRuns : 0.0;

        // pass^k = probability that all runs succeed
        // Formula: 1.0 if all passed, 0.0 otherwise
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

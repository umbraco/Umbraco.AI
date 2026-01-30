using System.Diagnostics;
using System.Text.Json;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Test execution orchestrator that runs tests, grades outcomes, and calculates metrics.
/// </summary>
internal class AiTestRunner : IAiTestRunner
{
    private readonly AiTestFeatureCollection _testFeatures;
    private readonly AiTestGraderCollection _graders;
    private readonly IAiTestRunRepository _runRepository;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;

    public AiTestRunner(
        AiTestFeatureCollection testFeatures,
        AiTestGraderCollection graders,
        IAiTestRunRepository runRepository,
        IBackOfficeSecurityAccessor securityAccessor)
    {
        _testFeatures = testFeatures;
        _graders = graders;
        _runRepository = runRepository;
        _securityAccessor = securityAccessor;
    }

    /// <inheritdoc />
    public async Task<AiTestMetrics> RunTestAsync(
        AiTest test,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        Guid? batchId = null,
        CancellationToken cancellationToken = default)
    {
        // Get the test feature for execution
        var testFeature = _testFeatures.GetById(test.TestTypeId)
            ?? throw new InvalidOperationException($"Test feature '{test.TestTypeId}' not found");

        var currentUserId = _securityAccessor.BackOfficeSecurity?.CurrentUser?.Key;
        var runs = new List<AiTestRun>();

        // Execute N runs
        for (int i = 1; i <= test.RunCount; i++)
        {
            var run = await ExecuteRunAsync(
                test,
                testFeature,
                i,
                profileIdOverride,
                contextIdsOverride,
                batchId,
                currentUserId,
                cancellationToken);

            runs.Add(run);
        }

        // Calculate metrics
        return CalculateMetrics(test.Id, runs);
    }

    private async Task<AiTestRun> ExecuteRunAsync(
        AiTest test,
        IAiTestFeature testFeature,
        int runNumber,
        Guid? profileIdOverride,
        IEnumerable<Guid>? contextIdsOverride,
        Guid? batchId,
        Guid? executedByUserId,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        AiTestTranscript? transcript = null;
        AiTestOutcome? outcome = null;
        AiTestRunStatus status = AiTestRunStatus.Failed;
        string? errorMessage = null;
        var graderResults = new List<AiTestGraderResult>();

        try
        {
            // Execute the test run
            transcript = await testFeature.ExecuteAsync(test, runNumber, cancellationToken);

            // Extract outcome from transcript
            outcome = ExtractOutcome(transcript);

            // Grade the outcome
            graderResults = await GradeOutcomeAsync(test, transcript, outcome, cancellationToken);

            // Determine overall status
            status = DetermineStatus(graderResults);
        }
        catch (Exception ex)
        {
            status = AiTestRunStatus.Error;
            errorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
        }

        // Create and save the run
        var run = new AiTestRun
        {
            TestId = test.Id,
            TestVersion = test.Version,
            RunNumber = runNumber,
            ProfileId = profileIdOverride ?? Guid.Empty, // TODO: Resolve actual profile from target
            ContextIdsJson = contextIdsOverride != null
                ? JsonSerializer.Serialize(contextIdsOverride)
                : null,
            ExecutedAt = DateTime.UtcNow,
            ExecutedByUserId = executedByUserId,
            DurationMs = stopwatch.ElapsedMilliseconds,
            Status = status,
            ErrorMessage = errorMessage,
            Outcome = outcome,
            Transcript = transcript,
            GraderResults = graderResults,
            BatchId = batchId
        };

        await _runRepository.AddAsync(run, cancellationToken);

        return run;
    }

    private static AiTestOutcome ExtractOutcome(AiTestTranscript transcript)
    {
        // Parse final output from transcript
        var outputJson = transcript.FinalOutputJson;

        // Determine output type and extract value
        // For now, assume text output - this can be enhanced based on transcript structure
        return new AiTestOutcome
        {
            OutputType = AiTestOutcomeType.Text,
            OutputValue = outputJson, // Simplified - actual implementation may parse JSON
            FinishReason = null,
            InputTokens = null,
            OutputTokens = null
        };
    }

    private async Task<List<AiTestGraderResult>> GradeOutcomeAsync(
        AiTest test,
        AiTestTranscript transcript,
        AiTestOutcome outcome,
        CancellationToken cancellationToken)
    {
        var results = new List<AiTestGraderResult>();

        foreach (var graderConfig in test.Graders)
        {
            var grader = _graders.GetById(graderConfig.GraderTypeId);
            if (grader == null)
            {
                // Grader not found - record as error
                results.Add(new AiTestGraderResult
                {
                    GraderId = graderConfig.Id,
                    Passed = false,
                    Score = null,
                    FailureMessage = $"Grader '{graderConfig.GraderTypeId}' not found"
                });
                continue;
            }

            try
            {
                var result = await grader.GradeAsync(transcript, outcome, graderConfig, cancellationToken);

                // Apply negate if configured
                if (graderConfig.Negate)
                {
                    result.GetType().GetProperty(nameof(AiTestGraderResult.Passed))!
                        .SetValue(result, !result.Passed);
                }

                results.Add(result);
            }
            catch (Exception ex)
            {
                // Grading error - record as failure
                results.Add(new AiTestGraderResult
                {
                    GraderId = graderConfig.Id,
                    Passed = false,
                    Score = null,
                    FailureMessage = $"Grading error: {ex.Message}"
                });
            }
        }

        return results;
    }

    private static AiTestRunStatus DetermineStatus(List<AiTestGraderResult> graderResults)
    {
        if (graderResults.Count == 0)
        {
            // No graders - consider passed
            return AiTestRunStatus.Passed;
        }

        // Check severity levels - errors must pass, warnings optional
        var errorGradersFailed = graderResults.Any(r =>
            !r.Passed && r.GraderId != Guid.Empty); // TODO: Get severity from grader config

        return errorGradersFailed ? AiTestRunStatus.Failed : AiTestRunStatus.Passed;
    }

    private static AiTestMetrics CalculateMetrics(Guid testId, List<AiTestRun> runs)
    {
        var totalRuns = runs.Count;

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
        var passedRuns = runs.Count(r =>
            r.Status == AiTestRunStatus.Passed ||
            (r.GraderResults.Count > 0 && r.GraderResults.Any(g => g.Passed)));

        // pass^k: Runs where ALL graders passed (or no graders)
        var allPassedRuns = runs.Count(r =>
            r.Status == AiTestRunStatus.Passed ||
            (r.GraderResults.Count > 0 && r.GraderResults.All(g => g.Passed)));

        return new AiTestMetrics
        {
            TestId = testId,
            TotalRuns = totalRuns,
            PassedRuns = passedRuns,
            PassAtK = (float)passedRuns / totalRuns,
            PassToTheK = (float)allPassedRuns / totalRuns,
            RunIds = runs.Select(r => r.Id).ToArray()
        };
    }
}

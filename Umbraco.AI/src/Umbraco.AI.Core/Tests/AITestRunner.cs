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
    public async Task<AITestRun> ExecuteTestAsync(
        AITest test,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(test);

        // Get the test feature
        var testFeature = _testFeatures.FirstOrDefault(f => f.Id == test.TestTypeId)
            ?? throw new InvalidOperationException($"Test feature '{test.TestTypeId}' not found");

        // Create test run
        var testRun = new AITestRun
        {
            Id = Guid.NewGuid(),
            TestId = test.Id,
            DateStarted = DateTime.UtcNow,
            ProfileIdOverride = profileIdOverride,
            ContextIdsOverride = contextIdsOverride?.ToList()
        };

        var transcripts = new List<AITestTranscript>();
        var outcomes = new List<AITestOutcome>();

        try
        {
            // Execute test N times
            for (var i = 0; i < test.RunCount; i++)
            {
                var runNumber = i + 1;

                // Execute the test feature
                var transcript = await testFeature.ExecuteAsync(
                    test,
                    runNumber,
                    profileIdOverride,
                    contextIdsOverride,
                    cancellationToken);

                // Set run ID
                transcript.RunId = testRun.Id;

                // Build outcome
                var outcome = new AITestOutcome
                {
                    Id = Guid.NewGuid(),
                    TranscriptId = transcript.Id,
                    RunNumber = runNumber,
                    FinalOutputJson = transcript.FinalOutputJson
                };

                // Grade the outcome using configured graders
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
                            FailureMessage = $"Grader implementation '{grader.GraderTypeId}' not found"
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

                // Set grader results
                outcome.GraderResults = graderResults;

                // Calculate aggregate pass/fail (only Error severity counts toward pass/fail)
                var errorGraders = graderResults.Where(r => r.Severity == AITestGraderSeverity.Error).ToList();
                outcome.Passed = errorGraders.Count == 0 || errorGraders.All(r => r.Passed);

                // Calculate weighted average score (across all graders)
                var totalWeight = test.Graders.Sum(g => g.Weight);
                if (totalWeight > 0)
                {
                    outcome.Score = graderResults
                        .Select((r, idx) => r.Score * test.Graders[idx].Weight)
                        .Sum() / totalWeight;
                }
                else
                {
                    outcome.Score = graderResults.Any() ? graderResults.Average(r => r.Score) : 0.0;
                }

                transcripts.Add(transcript);
                outcomes.Add(outcome);
            }

            // Calculate aggregate metrics
            testRun.TotalRuns = test.RunCount;
            testRun.PassedRuns = outcomes.Count(o => o.Passed);
            testRun.FailedRuns = outcomes.Count(o => !o.Passed);

            // Calculate pass@k score (proportion that passed at least once)
            testRun.PassAtK = testRun.TotalRuns > 0
                ? (double)testRun.PassedRuns / testRun.TotalRuns
                : 0.0;

            // Calculate average score across all runs
            testRun.AverageScore = outcomes.Any() ? outcomes.Average(o => o.Score) : 0.0;

            testRun.DateCompleted = DateTime.UtcNow;
            testRun.Status = AITestRunStatus.Completed;
        }
        catch (Exception ex)
        {
            testRun.DateCompleted = DateTime.UtcNow;
            testRun.Status = AITestRunStatus.Failed;
            testRun.ErrorMessage = ex.Message;
            testRun.ErrorStackTrace = ex.StackTrace;
        }

        // Set transcripts and outcomes
        testRun.Transcripts = transcripts;
        testRun.Outcomes = outcomes;

        // Persist the test run
        await _runRepository.SaveAsync(testRun, cancellationToken);

        return testRun;
    }
}

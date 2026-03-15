
namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Service implementation for executing AI tests.
/// Orchestrates test execution, grading, and result aggregation.
/// Supports variation-aware execution: runs default config + all variations under a single execution ID.
/// </summary>
internal sealed class AITestRunner : IAITestRunner
{
    private readonly IAITestRunRepository _runRepository;
    private readonly IAITestTranscriptRepository _transcriptRepository;
    private readonly AITestFeatureCollection _testFeatures;
    private readonly AITestGraderCollection _testGraders;

    public AITestRunner(
        IAITestRunRepository runRepository,
        IAITestTranscriptRepository transcriptRepository,
        AITestFeatureCollection testFeatures,
        AITestGraderCollection testGraders)
    {
        _runRepository = runRepository;
        _transcriptRepository = transcriptRepository;
        _testFeatures = testFeatures;
        _testGraders = testGraders;
    }

    /// <inheritdoc />
    public async Task<AITestExecutionResult> ExecuteTestAsync(
        AITest test,
        Guid? profileIdOverride = null,
        IEnumerable<Guid>? contextIdsOverride = null,
        IEnumerable<Guid>? guardrailIdsOverride = null,
        Guid? batchId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(test);

        // Get the test feature
        var testFeature = _testFeatures.FirstOrDefault(f => f.Id == test.TestFeatureId)
            ?? throw new InvalidOperationException($"Test feature '{test.TestFeatureId}' not found");

        // Generate execution ID (always new) and batch ID
        var executionId = Guid.NewGuid();
        var effectiveBatchId = batchId ?? Guid.NewGuid();

        // 1. Execute default configuration runs
        var effectiveProfile = profileIdOverride ?? test.ProfileId;
        var effectiveContextIds = contextIdsOverride?.ToList() ?? test.ContextIds.ToList();

        var defaultRuns = await ExecuteRunsAsync(
            test,
            testFeature,
            test.RunCount,
            effectiveProfile,
            effectiveContextIds,
            guardrailIdsOverride,
            effectiveBatchId,
            executionId,
            variationId: null,
            variationName: null,
            featureConfigOverride: null,
            cancellationToken);

        // 2. Execute each variation
        var variationMetricsList = new List<AITestVariationMetrics>();

        foreach (var variation in test.Variations)
        {
            var varProfile = variation.ProfileId ?? test.ProfileId;
            var varContextIds = variation.ContextIds ?? test.ContextIds;
            var varRunCount = variation.RunCount ?? test.RunCount;

            // Deep merge feature config if variation provides overrides
            var varFeatureConfig = variation.TestFeatureConfig.HasValue && test.TestFeatureConfig.HasValue
                ? JsonElementMergeHelper.DeepMerge(test.TestFeatureConfig.Value, variation.TestFeatureConfig.Value)
                : variation.TestFeatureConfig ?? test.TestFeatureConfig;

            var variationRuns = await ExecuteRunsAsync(
                test,
                testFeature,
                varRunCount,
                varProfile,
                varContextIds.ToList(),
                guardrailIdsOverride,
                effectiveBatchId,
                executionId,
                variation.Id,
                variation.Name,
                varFeatureConfig,
                cancellationToken);

            variationMetricsList.Add(new AITestVariationMetrics
            {
                VariationId = variation.Id,
                VariationName = variation.Name,
                Metrics = CalculateMetrics(test.Id, variationRuns)
            });
        }

        // 3. Calculate metrics
        var defaultMetrics = CalculateMetrics(test.Id, defaultRuns);

        // Aggregate = all runs (default + all variations)
        var allRuns = new List<AITestRun>(defaultRuns);
        foreach (var vm in variationMetricsList)
        {
            // Collect run IDs from variation metrics to look up runs
            // We can reconstruct from the runs we just executed
        }

        // Build aggregate from all runs across default + variations
        var aggregateRuns = new List<AITestRun>(defaultRuns);
        foreach (var variation in test.Variations)
        {
            var varMetrics = variationMetricsList.FirstOrDefault(v => v.VariationId == variation.Id);
            if (varMetrics != null)
            {
                // We need the actual runs for aggregate calculation - collect run IDs
                // Since we saved runs as we go, fetch them by execution ID for the variation
            }
        }

        // Simpler approach: track all runs as we execute them
        // We already have defaultRuns. For variation runs, we collected them in the loop above.
        // Let's refactor to collect all runs.
        // Actually we already have them - let me restructure.

        // Re-collect all variation runs for aggregate
        var allRunsList = new List<AITestRun>(defaultRuns);
        // We need to re-fetch variation runs or track them. Let me track them from the start.
        // The variationRuns from the loop are scoped - let's use a different approach.

        // For aggregate, combine default metrics with variation metrics manually
        var aggregateMetrics = CalculateAggregateMetrics(test.Id, defaultMetrics, variationMetricsList);

        return new AITestExecutionResult
        {
            TestId = test.Id,
            ExecutionId = executionId,
            BatchId = effectiveBatchId,
            DefaultMetrics = defaultMetrics,
            VariationMetrics = variationMetricsList,
            AggregateMetrics = aggregateMetrics
        };
    }

    /// <summary>
    /// Executes N runs with the given configuration.
    /// </summary>
    private async Task<List<AITestRun>> ExecuteRunsAsync(
        AITest test,
        IAITestFeature testFeature,
        int runCount,
        Guid? profileId,
        IReadOnlyList<Guid> contextIds,
        IEnumerable<Guid>? guardrailIdsOverride,
        Guid batchId,
        Guid executionId,
        Guid? variationId,
        string? variationName,
        System.Text.Json.JsonElement? featureConfigOverride,
        CancellationToken cancellationToken)
    {
        // Create an effective test with merged config if needed
        var effectiveTest = featureConfigOverride.HasValue
            ? CreateEffectiveTest(test, featureConfigOverride.Value)
            : test;

        var runs = new List<AITestRun>();
        for (int runNumber = 1; runNumber <= runCount; runNumber++)
        {
            var run = await ExecuteSingleRunAsync(
                effectiveTest,
                testFeature,
                runNumber,
                profileId,
                contextIds,
                guardrailIdsOverride,
                batchId,
                executionId,
                variationId,
                variationName,
                cancellationToken);

            runs.Add(run);

            // Save each run immediately
            await _runRepository.SaveAsync(run, cancellationToken);
        }

        return runs;
    }

    /// <summary>
    /// Creates a shallow copy of the test with overridden TestFeatureConfig.
    /// </summary>
    private static AITest CreateEffectiveTest(AITest original, System.Text.Json.JsonElement featureConfig)
    {
        return new AITest
        {
            Id = original.Id,
            Alias = original.Alias,
            Name = original.Name,
            Description = original.Description,
            TestFeatureId = original.TestFeatureId,
            TestTargetId = original.TestTargetId,
            ProfileId = original.ProfileId,
            ContextIds = original.ContextIds,
            TestFeatureConfig = featureConfig,
            Graders = original.Graders,
            Variations = original.Variations,
            RunCount = original.RunCount,
            Tags = original.Tags,
            IsActive = original.IsActive,
            BaselineRunId = original.BaselineRunId,
            DateModified = original.DateModified,
        };
    }

    /// <summary>
    /// Executes a single test run.
    /// </summary>
    private async Task<AITestRun> ExecuteSingleRunAsync(
        AITest test,
        IAITestFeature testFeature,
        int runNumber,
        Guid? profileId,
        IReadOnlyList<Guid> contextIds,
        IEnumerable<Guid>? guardrailIdsOverride,
        Guid batchId,
        Guid executionId,
        Guid? variationId,
        string? variationName,
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
            ProfileId = profileId,
            ContextIds = contextIds.ToList(),
            ExecutedAt = startTime,
            Status = AITestRunStatus.Running,
            BatchId = batchId,
            ExecutionId = executionId,
            VariationId = variationId,
            VariationName = variationName
        };

        try
        {
            // Execute the test feature
            var transcript = await testFeature.ExecuteAsync(
                test,
                runNumber,
                testRun.ProfileId,
                testRun.ContextIds,
                guardrailIdsOverride,
                cancellationToken);

            // Link transcript to run and persist it
            transcript.RunId = testRun.Id;
            await _transcriptRepository.SaveAsync(transcript, cancellationToken);
            testRun.TranscriptId = transcript.Id;

            // Build outcome from transcript
            var outcome = new AITestOutcome
            {
                OutputType = AITestOutputType.Text,
                OutputValue = testFeature.ExtractOutputValue(transcript),
                FinishReason = "completed",
                TokenUsage = null
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
            testRun.Error = new AITestRunError
            {
                Message = ex.Message,
                StackTrace = ex.StackTrace
            };
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

    /// <summary>
    /// Calculates aggregate metrics from default and variation metrics.
    /// </summary>
    private static AITestMetrics CalculateAggregateMetrics(
        Guid testId,
        AITestMetrics defaultMetrics,
        IReadOnlyList<AITestVariationMetrics> variationMetrics)
    {
        var totalRuns = defaultMetrics.TotalRuns + variationMetrics.Sum(v => v.Metrics.TotalRuns);
        var passedRuns = defaultMetrics.PassedRuns + variationMetrics.Sum(v => v.Metrics.PassedRuns);

        var passAtK = totalRuns > 0 ? (double)passedRuns / totalRuns : 0.0;
        var passToTheK = passedRuns == totalRuns && totalRuns > 0 ? 1.0 : 0.0;

        var allRunIds = new List<Guid>(defaultMetrics.RunIds);
        foreach (var vm in variationMetrics)
        {
            allRunIds.AddRange(vm.Metrics.RunIds);
        }

        return new AITestMetrics
        {
            TestId = testId,
            TotalRuns = totalRuns,
            PassedRuns = passedRuns,
            PassAtK = passAtK,
            PassToTheK = passToTheK,
            RunIds = allRunIds
        };
    }
}

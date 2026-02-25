using System.Text.Json;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.AI.Web.Api.Management.Provider.Models;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Api.Management.TestRun.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Test.Mapping;

/// <summary>
/// Defines mappings between test domain models and API models.
/// </summary>
public class TestMapDefinition : IMapDefinition
{
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // IAITestFeature -> TestFeatureResponseModel
        mapper.Define<IAITestFeature, TestFeatureResponseModel>((_, _) => new TestFeatureResponseModel(), MapTestFeature);

        // IAITestGrader -> TestGraderResponseModel
        mapper.Define<IAITestGrader, TestGraderResponseModel>((_, _) => new TestGraderResponseModel(), MapTestGrader);

        // AITest -> TestResponseModel
        mapper.Define<AITest, TestResponseModel>((source, context) => new TestResponseModel
        {
            Id = source.Id,
            Alias = source.Alias,
            Name = source.Name,
            Description = source.Description,
            TestFeatureId = source.TestFeatureId,
            TestTargetId = source.TestTargetId,
            TestFeatureConfig = source.TestFeatureConfig,
            Graders = source.Graders.Select(g => new TestGraderModel
            {
                Id = g.Id,
                GraderTypeId = g.GraderTypeId,
                Name = g.Name,
                Description = g.Description,
                Config = g.Config,
                Negate = g.Negate,
                Severity = g.Severity.ToString(),
                Weight = g.Weight
            }).ToList(),
            RunCount = source.RunCount,
            Tags = source.Tags,
            BaselineRunId = source.BaselineRunId,
            DateCreated = source.DateCreated,
            DateModified = source.DateModified,
            Version = source.Version
        });

        // AITest -> TestItemResponseModel
        mapper.Define<AITest, TestItemResponseModel>((source, context) => new TestItemResponseModel
        {
            Id = source.Id,
            Alias = source.Alias,
            Name = source.Name,
            Description = source.Description,
            TestFeatureId = source.TestFeatureId,
            Tags = source.Tags,
            RunCount = source.RunCount,
            DateCreated = source.DateCreated,
            DateModified = source.DateModified,
            Version = source.Version
        });

        // CreateTestRequestModel -> AITest
        mapper.Define<CreateTestRequestModel, AITest>((source, context) => new AITest
        {
            Alias = source.Alias,
            Name = source.Name,
            Description = source.Description,
            TestFeatureId = source.TestFeatureId,
            TestTargetId = source.TestTargetId,
            TestFeatureConfig = source.TestFeatureConfig,
            Graders = source.Graders.Select(g => new AITestGraderConfig
            {
                Id = g.Id,
                GraderTypeId = g.GraderTypeId,
                Name = g.Name,
                Description = g.Description,
                Config = g.Config,
                Negate = g.Negate,
                Severity = Enum.Parse<AITestGraderSeverity>(g.Severity, ignoreCase: true),
                Weight = g.Weight
            }).ToList(),
            RunCount = source.RunCount,
            Tags = source.Tags.ToList()
        });

        // UpdateTestRequestModel -> AITest (updates existing entity)
        // Note: Factory provides dummy values for required properties; MapFromUpdateRequest sets actual values
        mapper.Define<UpdateTestRequestModel, AITest>((_, _) => new AITest
        {
            Alias = string.Empty,
            Name = string.Empty,
            TestFeatureId = string.Empty,
            TestTargetId = Guid.Empty
        }, MapFromUpdateRequest);

        // AITestRun -> TestRunResponseModel
        mapper.Define<AITestRun, TestRunResponseModel>((source, context) =>
        {
            var test = context.HasItems
                && context.Items.TryGetValue("test", out var t)
                ? t as AITest : null;
            var graderConfigs = test?.Graders.ToDictionary(g => g.Id)
                ?? (context.HasItems && context.Items.TryGetValue("graderConfigs", out var cfgs)
                    ? cfgs as Dictionary<Guid, AITestGraderConfig> : null);
            var graderCollection = context.HasItems
                && context.Items.TryGetValue("graderCollection", out var coll)
                ? coll as AITestGraderCollection : null;

            return new TestRunResponseModel
            {
                Id = source.Id,
                TestId = source.TestId,
                TestName = test?.Name,
                TestVersion = source.TestVersion,
                RunNumber = source.RunNumber,
                ProfileId = source.ProfileId,
                ContextIds = source.ContextIds,
                ExecutedAt = source.ExecutedAt,
                ExecutedByUserId = source.ExecutedByUserId,
                Status = source.Status.ToString(),
                DurationMs = source.DurationMs,
                TranscriptId = source.TranscriptId,
                IsBaseline = test?.BaselineRunId == source.Id,
                Outcome = source.Outcome != null ? new TestOutcomeResponseModel
                {
                    OutputType = source.Outcome.OutputType.ToString(),
                    OutputValue = source.Outcome.OutputValue,
                    FinishReason = source.Outcome.FinishReason,
                    TokenUsageJson = source.Outcome.TokenUsageJson
                } : null,
                GraderResults = source.GraderResults.Select(r =>
                {
                    AITestGraderConfig? config = null;
                    graderConfigs?.TryGetValue(r.GraderId, out config);
                    var graderDef = config != null
                        ? graderCollection?.GetById(config.GraderTypeId) : null;

                    return new TestGraderResultResponseModel
                    {
                        GraderId = r.GraderId,
                        GraderName = config?.Name,
                        GraderTypeId = config?.GraderTypeId,
                        GraderType = graderDef?.Type.ToString(),
                        Weight = config?.Weight ?? 1.0,
                        Negate = config?.Negate ?? false,
                        Passed = r.Passed,
                        Score = r.Score,
                        ActualValue = r.ActualValue,
                        ExpectedValue = r.ExpectedValue,
                        FailureMessage = r.FailureMessage,
                        MetadataJson = r.MetadataJson,
                        Severity = r.Severity.ToString()
                    };
                }).ToList(),
                MetadataJson = source.MetadataJson,
                BatchId = source.BatchId
            };
        });

        // AITestTranscript -> TestTranscriptResponseModel
        mapper.Define<AITestTranscript, TestTranscriptResponseModel>((source, context) => new TestTranscriptResponseModel
        {
            Id = source.Id,
            MessagesJson = source.MessagesJson,
            ToolCallsJson = source.ToolCallsJson,
            ReasoningJson = source.ReasoningJson,
            TimingJson = source.TimingJson,
            FinalOutputJson = source.FinalOutputJson,
        });

        // AITestMetrics -> TestMetricsResponseModel
        mapper.Define<AITestMetrics, TestMetricsResponseModel>((source, context) => new TestMetricsResponseModel
        {
            TestId = source.TestId,
            TotalRuns = source.TotalRuns,
            PassedRuns = source.PassedRuns,
            PassAtK = source.PassAtK,
            PassToTheK = source.PassToTheK,
            RunIds = source.RunIds
        });

        // AITestRunComparison -> TestRunComparisonResponseModel
        mapper.Define<AITestRunComparison, TestRunComparisonResponseModel>((source, context) =>
        {
            var test = context.HasItems
                && context.Items.TryGetValue("test", out var t)
                ? t as AITest : null;
            var graderConfigs = test?.Graders.ToDictionary(g => g.Id)
                ?? (context.HasItems && context.Items.TryGetValue("graderConfigs", out var cfgs)
                    ? cfgs as Dictionary<Guid, AITestGraderConfig> : null);
            var graderCollection = context.HasItems
                && context.Items.TryGetValue("graderCollection", out var coll)
                ? coll as AITestGraderCollection : null;

            TestGraderResultResponseModel? MapGraderResult(AITestGraderResult? r)
            {
                if (r is null) return null;

                AITestGraderConfig? config = null;
                graderConfigs?.TryGetValue(r.GraderId, out config);
                var graderDef = config != null
                    ? graderCollection?.GetById(config.GraderTypeId) : null;

                return new TestGraderResultResponseModel
                {
                    GraderId = r.GraderId,
                    GraderName = config?.Name,
                    GraderTypeId = config?.GraderTypeId,
                    GraderType = graderDef?.Type.ToString(),
                    Weight = config?.Weight ?? 1.0,
                    Negate = config?.Negate ?? false,
                    Passed = r.Passed,
                    Score = r.Score,
                    ActualValue = r.ActualValue,
                    ExpectedValue = r.ExpectedValue,
                    FailureMessage = r.FailureMessage,
                    MetadataJson = r.MetadataJson,
                    Severity = r.Severity.ToString()
                };
            }

            return new TestRunComparisonResponseModel
            {
                BaselineRun = context.Map<TestRunResponseModel>(source.BaselineRun)!,
                ComparisonRun = context.Map<TestRunResponseModel>(source.ComparisonRun)!,
                IsRegression = source.IsRegression,
                IsImprovement = source.IsImprovement,
                DurationChangeMs = source.DurationChangeMs,
                GraderComparisons = source.GraderComparisons.Select(gc => new TestGraderComparisonResponseModel
                {
                    GraderId = gc.GraderId,
                    GraderName = gc.GraderName,
                    BaselineResult = MapGraderResult(gc.BaselineResult),
                    ComparisonResult = MapGraderResult(gc.ComparisonResult),
                    Changed = gc.Changed,
                    ScoreChange = gc.ScoreChange
                }).ToList()
            };
        });
    }

    // Umbraco.Code.MapAll -Id -TestFeatureId -IsActive -BaselineRunId -DateCreated -DateModified -Version -CreatedByUserId -ModifiedByUserId
    private static void MapFromUpdateRequest(UpdateTestRequestModel source, AITest target, MapperContext context)
    {
        // Note: Id, TestFeatureId, DateCreated are preserved from the existing entity
        // DateModified and Version will be set by the service/repository
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.Description = source.Description;
        target.TestTargetId = source.TestTargetId;
        target.TestFeatureConfig = source.TestFeatureConfig;
        target.Graders = source.Graders.Select(g => new AITestGraderConfig
        {
            Id = g.Id,
            GraderTypeId = g.GraderTypeId,
            Name = g.Name,
            Description = g.Description,
            Config = g.Config,
            Negate = g.Negate,
            Severity = Enum.Parse<AITestGraderSeverity>(g.Severity, ignoreCase: true),
            Weight = g.Weight
        }).ToList();
        target.RunCount = source.RunCount;
        target.Tags = source.Tags.ToList();
    }

    // Umbraco.Code.MapAll
    private static void MapTestFeature(IAITestFeature source, TestFeatureResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Category = source.Category;
        target.TestFeatureConfigSchema = source.ConfigType is not null
            ? context.Map<EditableModelSchemaModel>(source.GetConfigSchema())
            : null;
    }

    // Umbraco.Code.MapAll
    private static void MapTestGrader(IAITestGrader source, TestGraderResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Type = source.Type.ToString();
        target.ConfigSchema = source.ConfigType is not null
            ? context.Map<EditableModelSchemaModel>(source.GetConfigSchema())
            : null;
    }
}

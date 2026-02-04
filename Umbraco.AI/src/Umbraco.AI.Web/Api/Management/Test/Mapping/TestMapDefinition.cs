using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Test.Mapping;

/// <summary>
/// Defines mappings between test domain models and API models.
/// </summary>
public class TestMapDefinition : IMapDefinition
{
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // AITest -> TestResponseModel
        mapper.Define<AITest, TestResponseModel>((source, context) => new TestResponseModel
        {
            Id = source.Id,
            Alias = source.Alias,
            Name = source.Name,
            Description = source.Description,
            TestTypeId = source.TestTypeId,
            Target = new TestTargetModel
            {
                TargetId = source.Target.TargetId,
                IsAlias = source.Target.IsAlias
            },
            TestCaseJson = source.TestCase.TestCaseJson,
            Graders = source.Graders.Select(g => new TestGraderModel
            {
                Id = g.Id,
                GraderTypeId = g.GraderTypeId,
                Name = g.Name,
                Description = g.Description,
                ConfigJson = g.ConfigJson,
                Negate = g.Negate,
                Severity = g.Severity.ToString(),
                Weight = g.Weight
            }).ToList(),
            RunCount = source.RunCount,
            Tags = source.Tags,
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
            TestTypeId = source.TestTypeId,
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
            TestTypeId = source.TestTypeId,
            Target = new AITestTarget
            {
                TargetId = source.Target.TargetId,
                IsAlias = source.Target.IsAlias
            },
            TestCase = new AITestCase
            {
                TestCaseJson = source.TestCaseJson
            },
            Graders = source.Graders.Select(g => new AITestGrader
            {
                Id = g.Id,
                GraderTypeId = g.GraderTypeId,
                Name = g.Name,
                Description = g.Description,
                ConfigJson = g.ConfigJson,
                Negate = g.Negate,
                Severity = Enum.Parse<AITestGraderSeverity>(g.Severity, ignoreCase: true),
                Weight = g.Weight
            }).ToList(),
            RunCount = source.RunCount,
            Tags = source.Tags.ToList()
        });

        // UpdateTestRequestModel -> AITest
        mapper.Define<UpdateTestRequestModel, AITest>((source, context) => new AITest
        {
            Alias = source.Alias,
            Name = source.Name,
            Description = source.Description,
            TestTypeId = source.TestTypeId,
            Target = new AITestTarget
            {
                TargetId = source.Target.TargetId,
                IsAlias = source.Target.IsAlias
            },
            TestCase = new AITestCase
            {
                TestCaseJson = source.TestCaseJson
            },
            Graders = source.Graders.Select(g => new AITestGrader
            {
                Id = g.Id,
                GraderTypeId = g.GraderTypeId,
                Name = g.Name,
                Description = g.Description,
                ConfigJson = g.ConfigJson,
                Negate = g.Negate,
                Severity = Enum.Parse<AITestGraderSeverity>(g.Severity, ignoreCase: true),
                Weight = g.Weight
            }).ToList(),
            RunCount = source.RunCount,
            Tags = source.Tags.ToList()
        });

        // AITestRun -> TestRunResponseModel
        mapper.Define<AITestRun, TestRunResponseModel>((source, context) => new TestRunResponseModel
        {
            Id = source.Id,
            TestId = source.TestId,
            DateStarted = source.DateStarted,
            DateCompleted = source.DateCompleted,
            Status = source.Status.ToString(),
            TotalRuns = source.TotalRuns,
            PassedRuns = source.PassedRuns,
            FailedRuns = source.FailedRuns,
            PassAtK = source.PassAtK,
            AverageScore = source.AverageScore,
            ProfileIdOverride = source.ProfileIdOverride,
            ContextIdsOverride = source.ContextIdsOverride,
            ErrorMessage = source.ErrorMessage,
            Transcripts = source.Transcripts.Select(t => new TestTranscriptResponseModel
            {
                Id = t.Id,
                MessagesJson = t.MessagesJson,
                ToolCallsJson = t.ToolCallsJson,
                ReasoningJson = t.ReasoningJson,
                TimingJson = t.TimingJson,
                FinalOutputJson = t.FinalOutputJson
            }).ToList(),
            Outcomes = source.Outcomes.Select(o => new TestOutcomeResponseModel
            {
                Id = o.Id,
                RunNumber = o.RunNumber,
                Passed = o.Passed,
                Score = o.Score,
                GraderResults = o.GraderResults.Select(r => new TestGraderResultResponseModel
                {
                    GraderId = r.GraderId,
                    Passed = r.Passed,
                    Score = r.Score,
                    ActualValue = r.ActualValue,
                    ExpectedValue = r.ExpectedValue,
                    FailureMessage = r.FailureMessage,
                    MetadataJson = r.MetadataJson,
                    Severity = r.Severity.ToString()
                }).ToList()
            }).ToList()
        });
    }
}

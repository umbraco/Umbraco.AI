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
            TestVersion = source.TestVersion,
            RunNumber = source.RunNumber,
            ProfileId = source.ProfileId,
            ContextIds = source.ContextIds,
            ExecutedAt = source.ExecutedAt,
            ExecutedByUserId = source.ExecutedByUserId,
            Status = source.Status.ToString(),
            DurationMs = source.DurationMs,
            TranscriptId = source.TranscriptId,
            Outcome = source.Outcome != null ? new TestOutcomeResponseModel
            {
                OutputType = source.Outcome.OutputType.ToString(),
                OutputValue = source.Outcome.OutputValue,
                FinishReason = source.Outcome.FinishReason,
                TokenUsageJson = source.Outcome.TokenUsageJson
            } : null,
            GraderResults = source.GraderResults.Select(r => new TestGraderResultResponseModel
            {
                GraderId = r.GraderId,
                Passed = r.Passed,
                Score = r.Score,
                ActualValue = r.ActualValue,
                ExpectedValue = r.ExpectedValue,
                FailureMessage = r.FailureMessage,
                MetadataJson = r.MetadataJson,
                Severity = r.Severity.ToString()
            }).ToList(),
            MetadataJson = source.MetadataJson,
            BatchId = source.BatchId
        });
    }
}

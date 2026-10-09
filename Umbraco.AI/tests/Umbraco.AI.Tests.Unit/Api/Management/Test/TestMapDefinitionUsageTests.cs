// S3 — Compatible contract, persisted and exposed (AC3.2).
// Entry point: the real TestMapDefinition through UmbracoMapper, as in ChatMapDefinitionTests.
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.Test.Mapping;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Api.Management.Test;

public class TestMapDefinitionUsageTests
{
    public class GivenARunWithPopulatedUsage
    {
        private readonly AITestUsageEntry _firstModel = new()
        {
            Capability = AICapability.Chat,
            ProviderId = "openai",
            ModelId = "gpt-4o",
            ProfileId = Guid.NewGuid(),
            ProfileAlias = "chat-profile",
            FeatureType = "inline-chat",
            FeatureId = Guid.NewGuid(),
            FeatureAlias = "guardrail-llm-evaluator",
            InputTokens = 10,
            OutputTokens = 5,
            TotalTokens = 15,
            CallCount = 2,
            UnreportedCallCount = 0,
            DurationMs = 900,
            FailedCallCount = 1
        };

        private readonly TestUsageResponseModel _usage;
        private readonly TestOutcomeResponseModel _outcome;

        public GivenARunWithPopulatedUsage()
        {
            var mapper = new UmbracoMapper(
                new MapDefinitionCollection(() => new IMapDefinition[] { new TestMapDefinition() }),
                Mock.Of<ICoreScopeProvider>(),
                NullLogger<UmbracoMapper>.Instance);

            var run = new AITestRun
            {
                TestId = Guid.NewGuid(),
                Outcome = new AITestOutcome
                {
                    Usage = new AITestUsage
                    {
                        InputTokens = 14,
                        OutputTokens = 6,
                        TotalTokens = 20,
                        CallCount = 3,
                        UnreportedCallCount = 1,
                        DurationMs = 1200,
                        FailedCallCount = 1,
                        Breakdown =
                        [
                            _firstModel,
                            new AITestUsageEntry
                            {
                                Capability = AICapability.Embedding,
                                ModelId = "text-embedding-3-small",
                                CallCount = 1,
                                UnreportedCallCount = 1
                            }
                        ]
                    }
                }
            };

            var response = mapper.Map<TestRunResponseModel>(run)!;
            _outcome = response.Outcome!;
            _usage = _outcome.Usage!;
        }

        [Fact]
#pragma warning disable CS0618 // Asserting the obsolete property is never populated
        public void LeavesTheObsoleteTokenUsageNull() => _outcome.TokenUsage.ShouldBeNull();
#pragma warning restore CS0618

        [Fact]
        public void MapsTheCallCount() => _usage.CallCount.ShouldBe(3);

        [Fact]
        public void MapsTheUnreportedCallCount() => _usage.UnreportedCallCount.ShouldBe(1);

        [Fact]
        public void MapsTheDuration() => _usage.DurationMs.ShouldBe(1200);

        [Fact]
        public void MapsTheFailedCallCount() => _usage.FailedCallCount.ShouldBe(1);

        [Fact]
        public void MapsTheEntryDuration() => _usage.Breakdown.First().DurationMs.ShouldBe(900);

        [Fact]
        public void MapsTheEntryFailedCallCount() => _usage.Breakdown.First().FailedCallCount.ShouldBe(1);

        [Fact]
        public void MapsTheBreakdownEntries() => _usage.Breakdown.Count().ShouldBe(2);

        [Fact]
        public void MapsTheEntryIdentity()
        {
            var first = _usage.Breakdown.First();

            (first.Capability, first.ProviderId, first.ModelId, first.ProfileId, first.ProfileAlias)
                .ShouldBe((nameof(AICapability.Chat), _firstModel.ProviderId, _firstModel.ModelId, _firstModel.ProfileId, _firstModel.ProfileAlias));
        }

        [Fact]
        public void MapsTheFeatureIdentity()
        {
            var first = _usage.Breakdown.First();

            (first.FeatureType, first.FeatureId, first.FeatureAlias)
                .ShouldBe((_firstModel.FeatureType, _firstModel.FeatureId, _firstModel.FeatureAlias));
        }
    }
}

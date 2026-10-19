// S3 — Compatible contract, persisted and exposed (AC3.1, AC3.4).
using System.Text.Json;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Tests.Unit.Tests;

public class AITestUsageSerializationTests
{
    private static readonly JsonSerializerOptions Options = Umbraco.AI.Core.Constants.DefaultJsonSerializerOptions;

    public class GivenUsageWithTwoBreakdownEntries
    {
        private readonly AITestUsage _original;
        private readonly AITestUsage _loaded;

        public GivenUsageWithTwoBreakdownEntries()
        {
            _original = new AITestUsage
            {
                InputTokens = 130,
                OutputTokens = 45,
                TotalTokens = 175,
                CallCount = 3,
                UnreportedCallCount = 1,
                DurationMs = 1500,
                FailedCallCount = 1,
                Breakdown =
                [
                    new AITestUsageEntry
                    {
                        Capability = AICapability.Chat,
                        ProviderId = "openai",
                        ModelId = "gpt-x",
                        ProfileId = Guid.NewGuid(),
                        ProfileAlias = "p1",
                        FeatureType = "prompt",
                        FeatureId = Guid.NewGuid(),
                        FeatureAlias = "my-prompt",
                        InputTokens = 100,
                        OutputTokens = 20,
                        TotalTokens = 120,
                        CallCount = 2,
                        UnreportedCallCount = 0,
                        DurationMs = 1100,
                        FailedCallCount = 1
                    },
                    new AITestUsageEntry
                    {
                        Capability = AICapability.Embedding,
                        ProviderId = "openai",
                        ModelId = "embed-x",
                        ProfileId = Guid.NewGuid(),
                        ProfileAlias = "p2",
                        InputTokens = 30,
                        OutputTokens = 25,
                        TotalTokens = 55,
                        CallCount = 1,
                        UnreportedCallCount = 1
                    }
                ]
            };

            var json = JsonSerializer.Serialize(_original, Options);
            _loaded = JsonSerializer.Deserialize<AITestUsage>(json, Options)!;
        }

        [Fact]
        public void KeepsTheTotalTokens()
        {
            _loaded.TotalTokens.ShouldBe(_original.TotalTokens);
        }

        [Fact]
        public void KeepsTheCallCount()
        {
            _loaded.CallCount.ShouldBe(_original.CallCount);
        }

        [Fact]
        public void KeepsTheDuration()
        {
            _loaded.DurationMs.ShouldBe(_original.DurationMs);
        }

        [Fact]
        public void KeepsTheFailedCallCount()
        {
            _loaded.FailedCallCount.ShouldBe(_original.FailedCallCount);
        }

        [Fact]
        public void KeepsBothBreakdownEntries()
        {
            _loaded.Breakdown.Count.ShouldBe(2);
        }

        [Fact]
        public void KeepsTheEntryIdentity()
        {
            _loaded.Breakdown[0].ShouldBeEquivalentTo(_original.Breakdown[0]);
        }
    }

    public class GivenJsonWithTheOldModelsProperty
    {
        private readonly AITestUsage _loaded = JsonSerializer.Deserialize<AITestUsage>(
            """{"inputTokens":10,"outputTokens":5,"totalTokens":15,"models":[{"modelId":"gpt-x","totalTokens":15}]}""", Options)!;

        [Fact]
        public void LoadsWithAnEmptyBreakdown()
        {
            _loaded.Breakdown.ShouldBeEmpty();
        }
    }

    public class GivenJsonWrittenBeforeThisChange
    {
        private readonly AITestUsage _loaded;

        public GivenJsonWrittenBeforeThisChange()
        {
            _loaded = JsonSerializer.Deserialize<AITestUsage>(
                """{"inputTokens":10,"outputTokens":5,"totalTokens":15}""", Options)!;
        }

        [Fact]
        public void LoadsWithAnEmptyBreakdown()
        {
            _loaded.Breakdown.ShouldBeEmpty();
        }

        [Fact]
        public void LoadsWithCallCountZero()
        {
            _loaded.CallCount.ShouldBe(0);
        }

        [Fact]
        public void LoadsWithDurationZero()
        {
            _loaded.DurationMs.ShouldBe(0);
        }

        [Fact]
        public void LoadsWithFailedCallCountZero()
        {
            _loaded.FailedCallCount.ShouldBe(0);
        }
    }
}

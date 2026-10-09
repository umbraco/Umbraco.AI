using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;

// S1, S2 — Run token totals and breakdown at the collector level (AC1.9, AC1.11).
namespace Umbraco.AI.Tests.Unit.Observability;

public class AIUsageCollectorTests
{
    private static readonly Guid ProfileId = Guid.NewGuid();

    private static void Record(
        AIUsageCollector collector, string? modelId, UsageDetails? usage, long durationMs = 0, bool succeeded = true) =>
        collector.RecordCall(AICapability.Chat, "openai", modelId, ProfileId, "profile", null, null, null, usage, durationMs, succeeded);

    private static UsageDetails Usage(long total) =>
        new() { InputTokenCount = total / 2, OutputTokenCount = total - total / 2, TotalTokenCount = total };

    public class GivenTwoCallsToTheSameModel
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenTwoCallsToTheSameModel()
        {
            var collector = new AIUsageCollector();
            Record(collector, "gpt", Usage(10));
            Record(collector, "gpt", Usage(20));
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void GroupsThemIntoOneEntry() => _snapshot.Breakdown.Count.ShouldBe(1);

        [Fact]
        public void SumsTheirTokens() => _snapshot.TotalTokens.ShouldBe(30);
    }

    private static void RecordForFeature(
        AIUsageCollector collector, string? featureType, Guid? featureId, string? featureAlias, UsageDetails? usage) =>
        collector.RecordCall(AICapability.Chat, "openai", "gpt", ProfileId, "profile", featureType, featureId, featureAlias, usage, 0, true);

    public class GivenTwoCallsToTheSameModelFromDifferentFeatures
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenTwoCallsToTheSameModelFromDifferentFeatures()
        {
            var collector = new AIUsageCollector();
            RecordForFeature(collector, "prompt", Guid.NewGuid(), "my-prompt", Usage(10));
            RecordForFeature(collector, "inline-chat", null, "guardrail-llm-evaluator", Usage(20));
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void KeepsAnEntryPerFeature() => _snapshot.Breakdown.Count.ShouldBe(2);

        [Fact]
        public void KeepsOnlyItsOwnTokensInEachEntry() =>
            _snapshot.Breakdown.Single(e => e.FeatureType == "inline-chat").TotalTokens.ShouldBe(20);

        [Fact]
        public void TotalsTheSumOfTheEntries() =>
            _snapshot.TotalTokens.ShouldBe(_snapshot.Breakdown.Sum(e => e.TotalTokens));
    }

    public class GivenTwoCallsFromTheSameFeature
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenTwoCallsFromTheSameFeature()
        {
            var featureId = Guid.NewGuid();
            var collector = new AIUsageCollector();
            RecordForFeature(collector, "prompt", featureId, "my-prompt", Usage(10));
            RecordForFeature(collector, "prompt", featureId, "my-prompt", Usage(20));
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void GroupsThemIntoOneEntry() => _snapshot.Breakdown.Count.ShouldBe(1);

        [Fact]
        public void SumsTheirTokens() => _snapshot.Breakdown.Single().TotalTokens.ShouldBe(30);
    }

    public class GivenACallFromAFeature
    {
        private readonly AIUsageCollectorEntry _entry;
        private readonly Guid _featureId = Guid.NewGuid();

        public GivenACallFromAFeature()
        {
            var collector = new AIUsageCollector();
            RecordForFeature(collector, "prompt", _featureId, "my-prompt", Usage(10));
            _entry = collector.GetSnapshot().Breakdown.Single();
        }

        [Fact]
        public void CarriesTheFeatureType() => _entry.FeatureType.ShouldBe("prompt");

        [Fact]
        public void CarriesTheFeatureId() => _entry.FeatureId.ShouldBe(_featureId);

        [Fact]
        public void CarriesTheFeatureAlias() => _entry.FeatureAlias.ShouldBe("my-prompt");
    }

    public class GivenCallsToTwoModels
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenCallsToTwoModels()
        {
            var collector = new AIUsageCollector();
            Record(collector, "model-a", Usage(10));
            Record(collector, "model-b", Usage(20));
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void KeepsAnEntryPerModel() => _snapshot.Breakdown.Count.ShouldBe(2);

        [Fact]
        public void KeepsOnlyItsOwnTokensInEachEntry() =>
            _snapshot.Breakdown.Single(e => e.ModelId == "model-a").TotalTokens.ShouldBe(10);

        [Fact]
        public void TotalsTheSumOfTheEntries() =>
            _snapshot.TotalTokens.ShouldBe(_snapshot.Breakdown.Sum(e => e.TotalTokens));

        [Fact]
        public void OrdersEntriesByModel() =>
            _snapshot.Breakdown.Select(e => e.ModelId).ShouldBe(["model-a", "model-b"]);
    }

    public class GivenNoTotalFromTheProvider
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenNoTotalFromTheProvider()
        {
            var collector = new AIUsageCollector();
            Record(collector, "gpt", new UsageDetails { InputTokenCount = 7, OutputTokenCount = 3, TotalTokenCount = null });
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void UsesInputPlusOutputAsTheTotal() => _snapshot.TotalTokens.ShouldBe(10);
    }

    public class GivenACallWithNoUsage
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenACallWithNoUsage()
        {
            var collector = new AIUsageCollector();
            Record(collector, "gpt", null);
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void CountsItAsUnreported() => _snapshot.UnreportedCallCount.ShouldBe(1);

        [Fact]
        public void StillCountsTheCall() => _snapshot.CallCount.ShouldBe(1);
    }

    public class GivenTwoTimedCallsToTheSameModelAndOneToAnother
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenTwoTimedCallsToTheSameModelAndOneToAnother()
        {
            var collector = new AIUsageCollector();
            Record(collector, "model-a", Usage(10), durationMs: 100);
            Record(collector, "model-a", Usage(10), durationMs: 250);
            Record(collector, "model-b", Usage(10), durationMs: 40);
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void SumsTheDurationOfEachEntry() =>
            _snapshot.Breakdown.Single(e => e.ModelId == "model-a").DurationMs.ShouldBe(350);

        [Fact]
        public void TotalsTheDurationOfTheEntries() => _snapshot.DurationMs.ShouldBe(390);
    }

    public class GivenAFailedCallAmongSuccessfulOnes
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenAFailedCallAmongSuccessfulOnes()
        {
            var collector = new AIUsageCollector();
            Record(collector, "gpt", Usage(10));
            Record(collector, "gpt", Usage(10), succeeded: false);
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void CountsItInTheEntrysFailedCalls() => _snapshot.Breakdown.Single().FailedCallCount.ShouldBe(1);

        [Fact]
        public void CountsItInTheTotalFailedCalls() => _snapshot.FailedCallCount.ShouldBe(1);

        [Fact]
        public void StillCountsTheCall() => _snapshot.CallCount.ShouldBe(2);

        [Fact]
        public void DoesNotCountItAsUnreportedWhenItHadUsage() => _snapshot.UnreportedCallCount.ShouldBe(0);
    }

    public class GivenAFailedCallWithNoUsage
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenAFailedCallWithNoUsage()
        {
            var collector = new AIUsageCollector();
            Record(collector, "gpt", null, succeeded: false);
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void CountsItAsUnreported() => _snapshot.UnreportedCallCount.ShouldBe(1);

        [Fact]
        public void CountsItAsFailed() => _snapshot.FailedCallCount.ShouldBe(1);

        [Fact]
        public void StillCountsTheCall() => _snapshot.CallCount.ShouldBe(1);
    }

    public class GivenAUsageObjectWithNoCounts
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenAUsageObjectWithNoCounts()
        {
            var collector = new AIUsageCollector();
            Record(collector, "gpt", new UsageDetails());
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void CountsItAsUnreported() => _snapshot.UnreportedCallCount.ShouldBe(1);
    }

    public class GivenACallWithUnknownProviderAndModel
    {
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenACallWithUnknownProviderAndModel()
        {
            var collector = new AIUsageCollector();
            collector.RecordCall(AICapability.Chat, null, null, null, null, null, null, null, Usage(10), 0, true);
            _snapshot = collector.GetSnapshot();
        }

        [Fact]
        public void StillCountsTheCall() => _snapshot.CallCount.ShouldBe(1);

        [Fact]
        public void LandsInOneEntryWithANullModel() => _snapshot.Breakdown.Single().ModelId.ShouldBeNull();
    }

    public class GivenNestedScopes : IDisposable
    {
        private readonly AIUsageCollectionScope _outer;

        public GivenNestedScopes()
        {
            _outer = AIUsageCollectionScope.Begin();
            using var inner = AIUsageCollectionScope.Begin();
            Record(inner.Collector, "gpt", Usage(10));
        }

        public void Dispose() => _outer.Dispose();

        [Fact]
        public void RecordsTheCallOnlyInTheInnerCollector() =>
            _outer.Collector.GetSnapshot().CallCount.ShouldBe(0);

        [Fact]
        public void RestoresTheOuterCollectorAsCurrent() =>
            AIUsageCollectionScope.Current.ShouldBeSameAs(_outer.Collector);
    }

    public class GivenTwoConcurrentScopes
    {
        private readonly int[] _callCounts;

        public GivenTwoConcurrentScopes()
        {
            using var barrier = new Barrier(2);

            Task<int> Flow(int calls) => Task.Run(async () =>
            {
                using var scope = AIUsageCollectionScope.Begin();
                barrier.SignalAndWait(); // both scopes are open before either records
                for (var i = 0; i < calls; i++)
                {
                    await Task.Yield();
                    AIUsageCollectionScope.Current!.RecordCall(AICapability.Chat, "openai", "gpt", null, null, null, null, null, Usage(2), 0, true);
                }
                return scope.Collector.GetSnapshot().CallCount;
            });

            _callCounts = Task.WhenAll(Flow(3), Flow(5)).GetAwaiter().GetResult();
        }

        [Fact]
        public void KeepsEachFlowsCallsSeparate() => _callCounts.ShouldBe([3, 5]);
    }

    public class GivenNoOpenScope
    {
        [Fact]
        public void HasNoCurrentCollector() => AIUsageCollectionScope.Current.ShouldBeNull();
    }
}

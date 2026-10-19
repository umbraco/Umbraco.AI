// S1 — Run token totals: tracker feeds the collector (AC1.4).
// Entry point: the real AIOperationTracker / AIOperationScope, with collaborators mocked as in AIOperationTrackerTests.
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Observability;

public class AIOperationTrackerUsageCollectionTests
{
    private const string ModelId = "gpt-test";

    private static AIRuntimeContext CreateRuntimeContext()
    {
        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, Guid.NewGuid());
        runtimeContext.SetValue(Constants.ContextKeys.ProfileAlias, "test-profile");
        runtimeContext.SetValue(Constants.ContextKeys.ProviderId, "openai");
        runtimeContext.SetValue(Constants.ContextKeys.ModelId, ModelId);
        return runtimeContext;
    }

    private static AIOperationTracker CreateTracker(
        bool analyticsEnabled = true,
        AIRuntimeContext? runtimeContext = null,
        IAIUsageRecordFactory? usageRecordFactory = null)
    {
        runtimeContext ??= CreateRuntimeContext();

        var contextAccessor = new Mock<IAIRuntimeContextAccessor>();
        contextAccessor.Setup(x => x.Context).Returns(runtimeContext);

        var auditOptions = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        auditOptions.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = false });

        var analyticsOptions = new Mock<IOptionsMonitor<AIAnalyticsOptions>>();
        analyticsOptions.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions { Enabled = analyticsEnabled });

        return new AIOperationTracker(
            contextAccessor.Object,
            TestOperationRecorders.Default(new Mock<IAIAuditLogService>().Object, new Mock<IAIAuditLogFactory>().Object, auditOptions.Object, new Mock<IAIUsageRecordingService>().Object, usageRecordFactory ?? new Mock<IAIUsageRecordFactory>().Object, analyticsOptions.Object),
            NullLogger<AIOperationTracker>.Instance);
    }

    private static AIOperationDescriptor CreateDescriptor() => new()
    {
        Capability = AICapability.Chat,
        PromptData = "prompt data",
    };

    private static Task<AITrackedOperationResult<string>> TrackChatCallAsync(AIOperationTracker tracker) =>
        tracker.TrackAsync(
            CreateDescriptor(),
            _ => Task.FromResult(new AITrackedOperationResult<string>
            {
                Result = "ok",
                Usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 20, TotalTokenCount = 120 },
            }),
            CancellationToken.None);

    public class GivenATrackedCallInsideACollectionScope : IDisposable
    {
        private readonly AIUsageCollectionScope _scope = AIUsageCollectionScope.Begin();
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenATrackedCallInsideACollectionScope()
        {
            TrackChatCallAsync(CreateTracker()).GetAwaiter().GetResult();
            _snapshot = _scope.Collector.GetSnapshot();
        }

        [Fact]
        public void CollectsTheCallsTokens() => _snapshot.TotalTokens.ShouldBe(120);

        [Fact]
        public void CollectsTheModelFromTheRuntimeContext() => _snapshot.Breakdown.Single().ModelId.ShouldBe(ModelId);

        [Fact]
        public void CollectsTheCallAsSucceeded() => _snapshot.FailedCallCount.ShouldBe(0);

        public void Dispose() => _scope.Dispose();
    }

    public class GivenAnalyticsIsDisabled : IDisposable
    {
        private readonly AIUsageCollectionScope _scope = AIUsageCollectionScope.Begin();
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenAnalyticsIsDisabled()
        {
            TrackChatCallAsync(CreateTracker(analyticsEnabled: false)).GetAwaiter().GetResult();
            _snapshot = _scope.Collector.GetSnapshot();
        }

        [Fact]
        public void StillCollectsTheCall() => _snapshot.CallCount.ShouldBe(1);

        public void Dispose() => _scope.Dispose();
    }

    public class GivenAnalyticsIsEnabled : IDisposable
    {
        private readonly AIUsageCollectionScope _scope = AIUsageCollectionScope.Begin();
        private readonly AIUsageCollectorSnapshot _snapshot;
        private readonly List<AIUsageRecordResult> _recorded = [];

        public GivenAnalyticsIsEnabled()
        {
            var factory = new Mock<IAIUsageRecordFactory>();
            factory
                .Setup(x => x.Create(It.IsAny<AIUsageRecordContext>(), It.IsAny<AIUsageRecordResult>()))
                .Callback<AIUsageRecordContext, AIUsageRecordResult>((_, result) => _recorded.Add(result));

            TrackChatCallAsync(CreateTracker(usageRecordFactory: factory.Object)).GetAwaiter().GetResult();
            _snapshot = _scope.Collector.GetSnapshot();
        }

        [Fact]
        public void CollectsTheCall() => _snapshot.TotalTokens.ShouldBe(120);

        [Fact]
        public void RecordsTheSameCallToAnalytics() => _recorded.Single().Usage!.TotalTokenCount.ShouldBe(120);

        public void Dispose() => _scope.Dispose();
    }

    public class GivenATrackedCallThatTakesTime : IDisposable
    {
        private readonly AIUsageCollectionScope _scope = AIUsageCollectionScope.Begin();
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenATrackedCallThatTakesTime()
        {
            CreateTracker().TrackAsync(
                CreateDescriptor(),
                async _ =>
                {
                    await Task.Delay(50);
                    return new AITrackedOperationResult<string> { Result = "ok" };
                },
                CancellationToken.None).GetAwaiter().GetResult();
            _snapshot = _scope.Collector.GetSnapshot();
        }

        [Fact]
        public void CollectsTheMeasuredDuration() => _snapshot.DurationMs.ShouldBeGreaterThanOrEqualTo(40);

        public void Dispose() => _scope.Dispose();
    }

    public class GivenATrackedCallThatFails : IDisposable
    {
        private readonly AIUsageCollectionScope _scope = AIUsageCollectionScope.Begin();
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenATrackedCallThatFails()
        {
            var tracker = CreateTracker();
            var operation = tracker.BeginAsync(CreateDescriptor(), CancellationToken.None).GetAwaiter().GetResult();
            operation.FailAsync(
                new InvalidOperationException("boom"),
                new UsageDetails { InputTokenCount = 10, OutputTokenCount = 2, TotalTokenCount = 12 })
                .GetAwaiter().GetResult();

            _snapshot = _scope.Collector.GetSnapshot();
        }

        [Fact]
        public void CollectsThePartialUsage() => _snapshot.CallCount.ShouldBe(1);

        [Fact]
        public void CollectsThePartialTokens() => _snapshot.TotalTokens.ShouldBe(12);

        [Fact]
        public void CollectsTheCallAsFailed() => _snapshot.FailedCallCount.ShouldBe(1);

        public void Dispose() => _scope.Dispose();
    }

    public class GivenANestedCallChangesTheRuntimeContextMidFlight : IDisposable
    {
        private readonly AIUsageCollectionScope _scope = AIUsageCollectionScope.Begin();
        private readonly AIUsageCollectorSnapshot _snapshot;

        public GivenANestedCallChangesTheRuntimeContextMidFlight()
        {
            var runtimeContext = CreateRuntimeContext();
            var tracker = CreateTracker(runtimeContext: runtimeContext);

            tracker.TrackAsync(
                CreateDescriptor(),
                _ =>
                {
                    // A nested call (guardrail judge, embedding) overwrites the shared context.
                    runtimeContext.SetValue(Constants.ContextKeys.ModelId, "nested-model");
                    return Task.FromResult(new AITrackedOperationResult<string>
                    {
                        Result = "ok",
                        Usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 20, TotalTokenCount = 120 },
                    });
                },
                CancellationToken.None).GetAwaiter().GetResult();

            _snapshot = _scope.Collector.GetSnapshot();
        }

        [Fact]
        public void CollectsUnderTheModelAtBegin() => _snapshot.Breakdown.Single().ModelId.ShouldBe(ModelId);

        public void Dispose() => _scope.Dispose();
    }

    public class GivenANestedCallChangesTheFeatureMidFlight : IDisposable
    {
        private readonly AIUsageCollectionScope _scope = AIUsageCollectionScope.Begin();
        private readonly AIUsageCollectorEntry _entry;
        private readonly Guid _featureId = Guid.NewGuid();

        public GivenANestedCallChangesTheFeatureMidFlight()
        {
            var runtimeContext = CreateRuntimeContext();
            runtimeContext.SetValue(Constants.ContextKeys.FeatureType, "prompt");
            runtimeContext.SetValue(Constants.ContextKeys.FeatureId, _featureId);
            runtimeContext.SetValue(Constants.ContextKeys.FeatureAlias, "my-prompt");
            var tracker = CreateTracker(runtimeContext: runtimeContext);

            tracker.TrackAsync(
                CreateDescriptor(),
                _ =>
                {
                    // A nested guardrail judge call overwrites the shared feature identity.
                    runtimeContext.SetValue(Constants.ContextKeys.FeatureType, "inline-chat");
                    runtimeContext.SetValue(Constants.ContextKeys.FeatureId, Guid.NewGuid());
                    runtimeContext.SetValue(Constants.ContextKeys.FeatureAlias, "guardrail-llm-evaluator");
                    return Task.FromResult(new AITrackedOperationResult<string>
                    {
                        Result = "ok",
                        Usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 20, TotalTokenCount = 120 },
                    });
                },
                CancellationToken.None).GetAwaiter().GetResult();

            _entry = _scope.Collector.GetSnapshot().Breakdown.Single();
        }

        [Fact]
        public void CollectsUnderTheFeatureTypeAtBegin() => _entry.FeatureType.ShouldBe("prompt");

        [Fact]
        public void CollectsUnderTheFeatureIdAtBegin() => _entry.FeatureId.ShouldBe(_featureId);

        [Fact]
        public void CollectsUnderTheFeatureAliasAtBegin() => _entry.FeatureAlias.ShouldBe("my-prompt");

        public void Dispose() => _scope.Dispose();
    }

    public class GivenATrackedCallWithNoFeatureId : IDisposable
    {
        private readonly AIUsageCollectionScope _scope = AIUsageCollectionScope.Begin();
        private readonly AIUsageCollectorEntry _entry;

        public GivenATrackedCallWithNoFeatureId()
        {
            TrackChatCallAsync(CreateTracker()).GetAwaiter().GetResult();
            _entry = _scope.Collector.GetSnapshot().Breakdown.Single();
        }

        [Fact]
        public void CollectsItWithANullFeatureId() => _entry.FeatureId.ShouldBeNull();

        public void Dispose() => _scope.Dispose();
    }

    public class GivenNoCollectionScope
    {
        [Fact]
        public async Task CompletesNormally()
        {
            var result = await TrackChatCallAsync(CreateTracker());

            result.Result.ShouldBe("ok");
        }
    }
}

// Analytics credits a call to the model it began with, not to whatever a nested AI call left in the runtime context.
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

public class AIOperationTrackerAnalyticsIdentityTests
{
    private static readonly Guid OuterProfileId = Guid.NewGuid();

    private static AIRuntimeContext CreateOuterRuntimeContext()
    {
        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, OuterProfileId);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileAlias, "outer-profile");
        runtimeContext.SetValue(Constants.ContextKeys.ProviderId, "outer-provider");
        runtimeContext.SetValue(Constants.ContextKeys.ModelId, "outer-model");
        return runtimeContext;
    }

    /// <summary>Simulates a nested AI call (guardrail judge, embedding) overwriting the shared keys.</summary>
    private static void OverwriteAsNestedCall(AIRuntimeContext runtimeContext)
    {
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, Guid.NewGuid());
        runtimeContext.SetValue(Constants.ContextKeys.ProfileAlias, "nested-profile");
        runtimeContext.SetValue(Constants.ContextKeys.ProviderId, "nested-provider");
        runtimeContext.SetValue(Constants.ContextKeys.ModelId, "nested-model");
    }

    private sealed class Harness
    {
        public AIRuntimeContext RuntimeContext { get; } = CreateOuterRuntimeContext();
        public AIOperationTracker Tracker { get; }
        public TaskCompletionSource<AIUsageRecordContext> RecordedContext { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Harness()
        {
            var contextAccessor = new Mock<IAIRuntimeContextAccessor>();
            contextAccessor.Setup(x => x.Context).Returns(RuntimeContext);

            var auditOptions = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
            auditOptions.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = false });

            var analyticsOptions = new Mock<IOptionsMonitor<AIAnalyticsOptions>>();
            analyticsOptions.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions { Enabled = true });

            var recordFactory = new Mock<IAIUsageRecordFactory>();
            recordFactory
                .Setup(x => x.Create(It.IsAny<AIUsageRecordContext>(), It.IsAny<AIUsageRecordResult>()))
                .Returns((AIUsageRecordContext ctx, AIUsageRecordResult result) =>
                {
                    RecordedContext.TrySetResult(ctx);
                    return new AIUsageRecord
                    {
                        Id = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow,
                        Capability = ctx.Capability,
                        ProfileId = ctx.ProfileId,
                        ProfileAlias = ctx.ProfileAlias,
                        ProviderId = ctx.ProviderId,
                        ModelId = ctx.ModelId,
                        InputTokens = 0,
                        OutputTokens = 0,
                        TotalTokens = 0,
                        DurationMs = result.DurationMs,
                        Status = result.Succeeded ? AIUsageRecordStatus.Succeeded : AIUsageRecordStatus.Failed,
                        CreatedAt = DateTime.UtcNow,
                    };
                });

            var recordingService = new Mock<IAIUsageRecordingService>();
            recordingService
                .Setup(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            Tracker = new AIOperationTracker(
                contextAccessor.Object,
                TestOperationRecorders.Default(new Mock<IAIAuditLogService>().Object, new Mock<IAIAuditLogFactory>().Object, auditOptions.Object, recordingService.Object, recordFactory.Object, analyticsOptions.Object),
                NullLogger<AIOperationTracker>.Instance);
        }

        public AIUsageRecordContext AwaitRecordedContext()
        {
            RecordedContext.Task.Wait(TimeSpan.FromSeconds(2)).ShouldBeTrue("Timed out waiting for the usage record.");
            return RecordedContext.Task.Result;
        }
    }

    private static AIOperationDescriptor CreateDescriptor() => new()
    {
        Capability = AICapability.Chat,
        PromptData = "prompt data",
    };

    public class GivenANestedCallChangesTheRuntimeContextBeforeCompletion
    {
        private readonly AIUsageRecordContext _recorded;

        public GivenANestedCallChangesTheRuntimeContextBeforeCompletion()
        {
            var harness = new Harness();
            var operation = harness.Tracker.BeginAsync(CreateDescriptor(), CancellationToken.None).GetAwaiter().GetResult();
            OverwriteAsNestedCall(harness.RuntimeContext);

            operation.CompleteAsync(
                new UsageDetails { InputTokenCount = 10, OutputTokenCount = 2, TotalTokenCount = 12 },
                responseData: null).GetAwaiter().GetResult();

            _recorded = harness.AwaitRecordedContext();
        }

        [Fact]
        public void RecordsTheModelTheCallBeganWith() => _recorded.ModelId.ShouldBe("outer-model");

        [Fact]
        public void RecordsTheProviderTheCallBeganWith() => _recorded.ProviderId.ShouldBe("outer-provider");

        [Fact]
        public void RecordsTheProfileIdTheCallBeganWith() => _recorded.ProfileId.ShouldBe(OuterProfileId);

        [Fact]
        public void RecordsTheProfileAliasTheCallBeganWith() => _recorded.ProfileAlias.ShouldBe("outer-profile");
    }

    public class GivenANestedCallChangesTheRuntimeContextBeforeFailure
    {
        private readonly AIUsageRecordContext _recorded;

        public GivenANestedCallChangesTheRuntimeContextBeforeFailure()
        {
            var harness = new Harness();
            var operation = harness.Tracker.BeginAsync(CreateDescriptor(), CancellationToken.None).GetAwaiter().GetResult();
            OverwriteAsNestedCall(harness.RuntimeContext);

            operation.FailAsync(new InvalidOperationException("boom")).GetAwaiter().GetResult();

            _recorded = harness.AwaitRecordedContext();
        }

        [Fact]
        public void RecordsTheModelTheCallBeganWith() => _recorded.ModelId.ShouldBe("outer-model");

        [Fact]
        public void RecordsTheProviderTheCallBeganWith() => _recorded.ProviderId.ShouldBe("outer-provider");
    }
}

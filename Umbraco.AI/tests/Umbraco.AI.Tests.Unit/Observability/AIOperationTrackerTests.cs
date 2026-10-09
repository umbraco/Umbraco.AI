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

public class AIOperationTrackerTests
{
    private readonly Mock<IAIRuntimeContextAccessor> _contextAccessorMock;
    private readonly Mock<IAIAuditLogService> _auditLogServiceMock;
    private readonly Mock<IAIAuditLogFactory> _auditLogFactoryMock;
    private readonly Mock<IAIUsageRecordingService> _usageRecordingServiceMock;
    private readonly Mock<IAIUsageRecordFactory> _usageRecordFactoryMock;
    private readonly Mock<IOptionsMonitor<AIAuditLogOptions>> _auditLogOptionsMock;
    private readonly Mock<IOptionsMonitor<AIAnalyticsOptions>> _analyticsOptionsMock;
    private readonly AIRuntimeContext _runtimeContext;
    private readonly AIAuditLog _auditLog;

    public AIOperationTrackerTests()
    {
        _contextAccessorMock = new Mock<IAIRuntimeContextAccessor>();
        _auditLogServiceMock = new Mock<IAIAuditLogService>();
        _auditLogFactoryMock = new Mock<IAIAuditLogFactory>();
        _usageRecordingServiceMock = new Mock<IAIUsageRecordingService>();
        _usageRecordFactoryMock = new Mock<IAIUsageRecordFactory>();

        _auditLogOptionsMock = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        _auditLogOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = true });

        _analyticsOptionsMock = new Mock<IOptionsMonitor<AIAnalyticsOptions>>();
        _analyticsOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions { Enabled = true });

        // Real runtime context with ProfileId/Alias set so extraction succeeds.
        _runtimeContext = new AIRuntimeContext([]);
        _runtimeContext.SetValue(Constants.ContextKeys.ProfileId, Guid.NewGuid());
        _runtimeContext.SetValue(Constants.ContextKeys.ProfileAlias, "test-profile");
        _runtimeContext.SetValue(Constants.ContextKeys.ProviderId, "openai");
        _runtimeContext.SetValue(Constants.ContextKeys.ModelId, "gpt-test");
        _contextAccessorMock.Setup(x => x.Context).Returns(_runtimeContext);

        _auditLog = new AIAuditLog { Id = Guid.NewGuid() };
        _auditLogFactoryMock
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Returns(_auditLog);

        _auditLogServiceMock
            .Setup(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        _auditLogServiceMock
            .Setup(x => x.QueueCompleteAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        _auditLogServiceMock
            .Setup(x => x.QueueRecordAuditLogFailureAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        _usageRecordFactoryMock
            .Setup(x => x.Create(It.IsAny<AIUsageRecordContext>(), It.IsAny<AIUsageRecordResult>()))
            .Returns((AIUsageRecordContext ctx, AIUsageRecordResult result) => BuildUsageRecord(ctx, result));

        _usageRecordingServiceMock
            .Setup(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
    }

    // Test 1: successful op queues start + complete audit and one usage record.
    [Fact]
    public async Task TrackAsync_OnSuccess_QueuesStartCompleteAudit_AndUsage()
    {
        // Arrange
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor();
        var usageSignal = ArrangeUsageRecordingSignal();
        var usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5, TotalTokenCount = 15 };

        // Act
        var result = await tracker.TrackAsync(
            descriptor,
            _ => Task.FromResult(new AITrackedOperationResult<string>
            {
                Result = "success",
                Usage = usage,
                ResponseData = "ok",
            }),
            CancellationToken.None);

        await AwaitOrTimeout(usageSignal.Task);

        // Assert
        result.Result.ShouldBe("success");
        _auditLogServiceMock.Verify(x => x.QueueStartAuditLogAsync(_auditLog, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.Is<AIAuditResponse?>(r => r != null && (string?)r.Data == "ok" && r.Usage == usage),
            It.IsAny<CancellationToken>()), Times.Once);
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test 2: exception path queues audit failure + a failed usage record, then rethrows.
    [Fact]
    public async Task TrackAsync_OnException_QueuesAuditFailure_AndFailedUsage_AndRethrows()
    {
        // Arrange
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor();
        var usageSignal = ArrangeUsageRecordingSignal();
        var exception = new InvalidOperationException("boom");

        // Act
        await Should.ThrowAsync<InvalidOperationException>(() =>
            tracker.TrackAsync<string>(
                descriptor,
                _ => Task.FromException<AITrackedOperationResult<string>>(exception),
                CancellationToken.None));

        var record = await AwaitOrTimeout(usageSignal.Task);

        // Assert
        _auditLogServiceMock.Verify(x => x.QueueRecordAuditLogFailureAsync(_auditLog, It.IsAny<AIAuditPrompt?>(), exception, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()), Times.Never);
        record.Status.ShouldBe(AIUsageRecordStatus.Failed);
        record.ErrorMessage.ShouldBe("boom");
    }

    // Test 3: audit disabled => no audit queue calls, usage still recorded.
    [Fact]
    public async Task TrackAsync_AuditDisabled_SkipsAudit_ButRecordsUsage()
    {
        // Arrange
        _auditLogOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = false });
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor();
        var usageSignal = ArrangeUsageRecordingSignal();

        // Act
        await tracker.TrackAsync(
            descriptor,
            _ => Task.FromResult(new AITrackedOperationResult<string>
            {
                Result = "success",
                Usage = new UsageDetails { InputTokenCount = 1, OutputTokenCount = 1, TotalTokenCount = 2 },
            }),
            CancellationToken.None);

        await AwaitOrTimeout(usageSignal.Task);

        // Assert
        _auditLogFactoryMock.Verify(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()), Times.Never);
        _auditLogServiceMock.Verify(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test 4: analytics disabled => no usage record, audit still queued.
    [Fact]
    public async Task TrackAsync_AnalyticsDisabled_SkipsUsage_ButQueuesAudit()
    {
        // Arrange
        _analyticsOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions { Enabled = false });
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor();

        // Act
        await tracker.TrackAsync(
            descriptor,
            _ => Task.FromResult(new AITrackedOperationResult<string>
            {
                Result = "success",
                Usage = new UsageDetails { InputTokenCount = 1, OutputTokenCount = 1, TotalTokenCount = 2 },
            }),
            CancellationToken.None);

        await EnsureNoUsageRecorded();

        // Assert
        _auditLogServiceMock.Verify(x => x.QueueStartAuditLogAsync(_auditLog, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(_auditLog, It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()), Times.Once);
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Test 6: null Usage => usage record still queued (duration and status only).
    [Fact]
    public async Task TrackAsync_NullUsage_StillRecordsUsage()
    {
        // Arrange
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor();
        var usageSignal = ArrangeUsageRecordingSignal();

        // Act
        await tracker.TrackAsync(
            descriptor,
            _ => Task.FromResult(new AITrackedOperationResult<string> { Result = "success", Usage = null }),
            CancellationToken.None);

        var record = await AwaitOrTimeout(usageSignal.Task);

        // Assert
        record.InputTokens.ShouldBe(0);
        record.OutputTokens.ShouldBe(0);
        record.TotalTokens.ShouldBe(0);
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test 7: complete/failure audit uses CancellationToken.None even if the passed token is cancelled.
    [Fact]
    public async Task TrackAsync_UsesCancellationTokenNone_ForStatusPersistence()
    {
        // Arrange
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act — success path with an already-cancelled token.
        await tracker.TrackAsync(
            descriptor,
            _ => Task.FromResult(new AITrackedOperationResult<string> { Result = "success" }),
            cts.Token);

        // Assert — status update must use CancellationToken.None so it isn't skipped on disconnects.
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog, It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), CancellationToken.None), Times.Once);

        // Act — failure path with an already-cancelled token.
        var exception = new InvalidOperationException("boom");
        await Should.ThrowAsync<InvalidOperationException>(() =>
            tracker.TrackAsync<string>(
                descriptor,
                _ => Task.FromException<AITrackedOperationResult<string>>(exception),
                cts.Token));

        // Assert — failure must also be recorded with CancellationToken.None.
        _auditLogServiceMock.Verify(x => x.QueueRecordAuditLogFailureAsync(
            _auditLog, It.IsAny<AIAuditPrompt?>(), exception, CancellationToken.None), Times.Once);
    }

    // #531: a cancelled call still gets its usage record, marked failed with the usage it reported.
    [Fact]
    public async Task TrackAsync_WhenTheCallIsCancelled_StillQueuesAFailedUsageRecord()
    {
        // Arrange
        var tracker = CreateTracker();
        var queuedWith = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        AIUsageRecord? queued = null;
        _usageRecordingServiceMock
            .Setup(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()))
            .Callback<AIUsageRecord, CancellationToken>((record, token) =>
            {
                queued = record;
                queuedWith.TrySetResult(token);
            })
            .Returns((AIUsageRecord _, CancellationToken token) =>
                token.IsCancellationRequested ? ValueTask.FromCanceled(token) : ValueTask.CompletedTask);
        using var cts = new CancellationTokenSource();

        // Act
        await Should.ThrowAsync<OperationCanceledException>(() =>
            tracker.TrackAsync<string>(
                CreateDescriptor(),
                ct =>
                {
                    cts.Cancel();
                    ct.ThrowIfCancellationRequested();
                    return Task.FromResult(new AITrackedOperationResult<string> { Result = "never" });
                },
                cts.Token));

        // Assert
        (await queuedWith.Task.WaitAsync(TimeSpan.FromSeconds(2))).ShouldBe(CancellationToken.None);
        queued.ShouldNotBeNull();
        queued.Status.ShouldBe(AIUsageRecordStatus.Failed);
    }

    // Test 8: audit log is created with parentId = AIAuditScope.Current when nested.
    [Fact]
    public async Task TrackAsync_NestedScope_ParentsAuditLog()
    {
        // Arrange
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor();
        var parentAuditLogId = Guid.NewGuid();

        // Act
        using (AIAuditScope.Begin(parentAuditLogId))
        {
            await tracker.TrackAsync(
                descriptor,
                _ => Task.FromResult(new AITrackedOperationResult<string> { Result = "success" }),
                CancellationToken.None);
        }

        // Assert
        _auditLogFactoryMock.Verify(x => x.Create(
            It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), parentAuditLogId), Times.Once);
    }

    // #529: the start is queued before this call's own scope begins, so the service's ambient-parent
    // fallback can't pick up the entry itself; the scope is still open while the operation runs.
    [Fact]
    public async Task TrackAsync_TopLevel_QueuesStartBeforeItsOwnScopeBegins()
    {
        // Arrange
        var tracker = CreateTracker();
        Guid? scopeAtQueueStart = Guid.NewGuid();
        Guid? scopeDuringOperation = null;
        _auditLogServiceMock
            .Setup(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback(() => scopeAtQueueStart = AIAuditScope.Current?.AuditLogId)
            .Returns(ValueTask.CompletedTask);

        // Act
        await tracker.TrackAsync(
            CreateDescriptor(),
            _ =>
            {
                scopeDuringOperation = AIAuditScope.Current?.AuditLogId;
                return Task.FromResult(new AITrackedOperationResult<string> { Result = "success" });
            },
            CancellationToken.None);

        // Assert
        scopeAtQueueStart.ShouldBeNull();
        scopeDuringOperation.ShouldBe(_auditLog.Id);
    }

    // #529: a call made inside another tracked call is parented to it; the outer call has no parent.
    [Fact]
    public async Task TrackAsync_CallInsideAnotherTrackedCall_IsParentedToIt()
    {
        // Arrange
        var tracker = CreateTracker();
        var created = new List<(AIAuditLog Log, Guid? ParentId)>();
        _auditLogFactoryMock
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Returns((AIAuditContext _, IReadOnlyDictionary<string, string>? _, Guid? parentId) =>
            {
                var log = new AIAuditLog { Id = Guid.NewGuid() };
                created.Add((log, parentId));
                return log;
            });

        // Act
        await tracker.TrackAsync(
            CreateDescriptor(),
            async ct =>
            {
                await Task.Yield();
                await tracker.TrackAsync(
                    CreateDescriptor(),
                    _ => Task.FromResult(new AITrackedOperationResult<string> { Result = "inner" }),
                    ct);
                return new AITrackedOperationResult<string> { Result = "outer" };
            },
            CancellationToken.None);

        // Assert
        created.Count.ShouldBe(2);
        created[0].ParentId.ShouldBeNull();
        created[1].ParentId.ShouldBe(created[0].Log.Id);
    }

    // Test 9: BeginAsync + CompleteAsync mirrors TrackAsync success behavior.
    [Fact]
    public async Task BeginThenComplete_QueuesStartCompleteAudit_AndUsage()
    {
        // Arrange
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor();
        var usageSignal = ArrangeUsageRecordingSignal();
        var usage = new UsageDetails { InputTokenCount = 3, OutputTokenCount = 7, TotalTokenCount = 10 };

        // Act
        var scope = await tracker.BeginAsync(descriptor, CancellationToken.None);
        await scope.CompleteAsync(usage, "streamed result");

        await AwaitOrTimeout(usageSignal.Task);

        // Assert
        _auditLogServiceMock.Verify(x => x.QueueStartAuditLogAsync(_auditLog, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.Is<AIAuditResponse?>(r => r != null && (string?)r.Data == "streamed result" && r.Usage == usage),
            It.IsAny<CancellationToken>()), Times.Once);
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test 10: usage recording exception is swallowed (does not throw out of TrackAsync).
    [Fact]
    public async Task TrackAsync_UsageRecordingThrows_DoesNotPropagate()
    {
        // Arrange
        _usageRecordFactoryMock
            .Setup(x => x.Create(It.IsAny<AIUsageRecordContext>(), It.IsAny<AIUsageRecordResult>()))
            .Throws(new InvalidOperationException("usage recording exploded"));

        var tracker = CreateTracker();
        var descriptor = CreateDescriptor();

        // Act
        var result = await tracker.TrackAsync(
            descriptor,
            _ => Task.FromResult(new AITrackedOperationResult<string>
            {
                Result = "success",
                Usage = new UsageDetails { InputTokenCount = 1, OutputTokenCount = 1, TotalTokenCount = 2 },
            }),
            CancellationToken.None);

        // Assert — the outer operation must not observe the usage-recording failure.
        result.Result.ShouldBe("success");
        _usageRecordFactoryMock.Verify(x => x.Create(It.IsAny<AIUsageRecordContext>(), It.IsAny<AIUsageRecordResult>()), Times.Once);
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TrackAsync_WithNoRuntimeContext_RunsTheCallWithoutAuditOrUsageRecord()
    {
        // Arrange
        _contextAccessorMock.Setup(x => x.Context).Returns((AIRuntimeContext?)null);
        var tracker = CreateTracker();

        // Act
        var result = await tracker.TrackAsync(
            CreateDescriptor(),
            _ => Task.FromResult(new AITrackedOperationResult<string>
            {
                Result = "success",
                Usage = new UsageDetails { InputTokenCount = 1, OutputTokenCount = 1, TotalTokenCount = 2 },
            }),
            CancellationToken.None);
        await EnsureNoUsageRecorded();

        // Assert
        result.Result.ShouldBe("success");
        _auditLogServiceMock.Verify(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TrackAsync_QueuesTheAuditStartBeforeTheOperationRuns()
    {
        // Arrange
        var tracker = CreateTracker();
        var startQueuedWhenOperationRan = false;

        // Act
        await tracker.TrackAsync(
            CreateDescriptor(),
            _ =>
            {
                startQueuedWhenOperationRan = _auditLogServiceMock.Invocations
                    .Any(i => i.Method.Name == nameof(IAIAuditLogService.QueueStartAuditLogAsync));
                return Task.FromResult(new AITrackedOperationResult<string> { Result = "success" });
            },
            CancellationToken.None);

        // Assert
        startQueuedWhenOperationRan.ShouldBeTrue();
    }

    // #562: the tags go on the call's own gen_ai span, not on the caller's span around it.
    [Fact]
    public async Task TrackAsync_WithAudit_TagsTheCallsSpanFromTheAuditEntry_AndSetsItsTraceId()
    {
        // Arrange
        var tracker = CreateTracker();
        using var callerSpan = new System.Diagnostics.Activity("caller").Start();

        // Act
        var span = await TrackModelCallAsync(tracker);

        // Assert
        span.GetTagItem(Umbraco.AI.Core.Telemetry.AITelemetry.Tags.AuditId).ShouldBe(_auditLog.Id.ToString());
        callerSpan.GetTagItem(Umbraco.AI.Core.Telemetry.AITelemetry.Tags.AuditId).ShouldBeNull();
        _auditLog.TraceId.ShouldBe(callerSpan.TraceId.ToString());
    }

    [Fact]
    public async Task TrackAsync_WithAuditDisabled_TagsTheCallsSpanFromTheRuntimeContext()
    {
        // Arrange
        _auditLogOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = false });
        var tracker = CreateTracker();
        using var callerSpan = new System.Diagnostics.Activity("caller").Start();

        // Act
        var span = await TrackModelCallAsync(tracker);

        // Assert
        span.GetTagItem(Umbraco.AI.Core.Telemetry.AITelemetry.Tags.ProfileAlias).ShouldBe("test-profile");
        span.GetTagItem(Umbraco.AI.Core.Telemetry.AITelemetry.Tags.AuditId).ShouldBeNull();
        callerSpan.GetTagItem(Umbraco.AI.Core.Telemetry.AITelemetry.Tags.ProfileAlias).ShouldBeNull();
    }

    [Fact]
    public async Task TrackAsync_NestedCall_TagsItsOwnSpanWithItsOwnProfile()
    {
        // Arrange
        _auditLogOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = false });
        var tracker = CreateTracker();
        System.Diagnostics.Activity? nestedSpan = null;

        // Act: a nested call (e.g. a guardrail judge) runs for another profile, outside the parent's own span.
        var parentSpan = await TrackModelCallAsync(tracker, async () =>
        {
            _runtimeContext.SetValue(Constants.ContextKeys.ProfileAlias, "judge-profile");
            nestedSpan = await TrackModelCallAsync(tracker);
            _runtimeContext.SetValue(Constants.ContextKeys.ProfileAlias, "test-profile");
        });

        // Assert
        parentSpan.GetTagItem(Umbraco.AI.Core.Telemetry.AITelemetry.Tags.ProfileAlias).ShouldBe("test-profile");
        nestedSpan.ShouldNotBeNull();
        nestedSpan.GetTagItem(Umbraco.AI.Core.Telemetry.AITelemetry.Tags.ProfileAlias).ShouldBe("judge-profile");
    }

    // Decision 1 in docs/plans/tracking-recorders: a recorder that fails must not fail the AI call. Before
    // the audit log moved into a recorder, this exception escaped into the call.
    [Fact]
    public async Task TrackAsync_WhenTheAuditEntryCantBeCreated_StillRunsAndRecordsTheCall()
    {
        // Arrange
        _auditLogFactoryMock
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Throws(new ArgumentException("ProfileId must be set in the AIAuditContext."));
        var usageSignal = ArrangeUsageRecordingSignal();
        var tracker = CreateTracker();

        // Act
        var result = await tracker.TrackAsync(
            CreateDescriptor(),
            _ => Task.FromResult(new AITrackedOperationResult<string> { Result = "success" }),
            CancellationToken.None);

        // Assert
        result.Result.ShouldBe("success");
        (await AwaitOrTimeout(usageSignal.Task)).Status.ShouldBe(AIUsageRecordStatus.Succeeded);
        _auditLogServiceMock.Verify(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Decision 5: a guardrail block is its own outcome, so analytics stores "Blocked" as audit does.
    [Fact]
    public async Task TrackAsync_WhenAGuardrailBlocksTheCall_RecordsItAsBlocked()
    {
        // Arrange
        AIUsageRecordResult? recorded = null;
        var usageSignal = ArrangeUsageRecordingSignal();
        _usageRecordFactoryMock
            .Setup(x => x.Create(It.IsAny<AIUsageRecordContext>(), It.IsAny<AIUsageRecordResult>()))
            .Callback<AIUsageRecordContext, AIUsageRecordResult>((_, result) => recorded = result)
            .Returns((AIUsageRecordContext ctx, AIUsageRecordResult result) => BuildUsageRecord(ctx, result));
        var blocked = new Umbraco.AI.Core.Guardrails.AIGuardrailBlockedException(
            new Umbraco.AI.Core.Guardrails.Evaluators.AIGuardrailEvaluationResult
            {
                Action = Umbraco.AI.Core.Guardrails.AIGuardrailAction.Block,
                Phase = Umbraco.AI.Core.Guardrails.AIGuardrailPhase.PreGenerate,
                RuleResults = [],
            });
        var tracker = CreateTracker();

        // Act
        await Should.ThrowAsync<Umbraco.AI.Core.Guardrails.AIGuardrailBlockedException>(() =>
            tracker.TrackAsync<string>(
                CreateDescriptor(),
                _ => Task.FromException<AITrackedOperationResult<string>>(blocked),
                CancellationToken.None));
        await AwaitOrTimeout(usageSignal.Task);

        // Assert
        recorded.ShouldNotBeNull();
        recorded.Succeeded.ShouldBeFalse();
        recorded.Blocked.ShouldBeTrue();
    }

    // The audit entry is built from the identity the tracker captured, not a second read of the context.
    [Fact]
    public async Task TrackAsync_BuildsTheAuditContextFromTheCapturedIdentity()
    {
        // Arrange
        var featureId = Guid.NewGuid();
        _runtimeContext.SetValue(Constants.ContextKeys.ProfileVersion, 3);
        _runtimeContext.SetValue(Constants.ContextKeys.FeatureType, "prompt");
        _runtimeContext.SetValue(Constants.ContextKeys.FeatureId, featureId);
        _runtimeContext.SetValue(Constants.ContextKeys.FeatureVersion, 7);
        _runtimeContext.SetValue(Constants.ContextKeys.EntityId, "entity-1");
        _runtimeContext.SetValue(Constants.ContextKeys.EntityType, "document");
        AIAuditContext? captured = null;
        _auditLogFactoryMock
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Callback<AIAuditContext, IReadOnlyDictionary<string, string>?, Guid?>((context, _, _) => captured = context)
            .Returns(_auditLog);
        var tracker = CreateTracker();

        // Act
        await tracker.TrackAsync(
            CreateDescriptor(),
            _ => Task.FromResult(new AITrackedOperationResult<string> { Result = "success" }),
            CancellationToken.None);

        // Assert
        captured.ShouldNotBeNull();
        captured.ProfileAlias.ShouldBe("test-profile");
        captured.ProviderId.ShouldBe("openai");
        captured.ModelId.ShouldBe("gpt-test");
        captured.ProfileVersion.ShouldBe(3);
        captured.FeatureType.ShouldBe("prompt");
        captured.FeatureId.ShouldBe(featureId);
        captured.FeatureVersion.ShouldBe(7);
        captured.EntityId.ShouldBe("entity-1");
        captured.EntityType.ShouldBe("document");
        captured.Prompt.ShouldBe("prompt data");
    }

    // Log keys are read by the tracker for every capability; image calls used to pass none.
    [Fact]
    public async Task TrackAsync_PassesTheDeclaredLogValuesToTheAuditEntry_ForAnyCapability()
    {
        // Arrange
        _runtimeContext.SetValue(Constants.ContextKeys.LogKeys, new[] { "RunId" });
        _runtimeContext.SetValue("RunId", "run-42");
        IReadOnlyDictionary<string, string>? captured = null;
        _auditLogFactoryMock
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Callback<AIAuditContext, IReadOnlyDictionary<string, string>?, Guid?>((_, metadata, _) => captured = metadata)
            .Returns(_auditLog);
        var tracker = CreateTracker();

        // Act
        await tracker.TrackAsync(
            new AIOperationDescriptor { Capability = AICapability.ImageGeneration, PromptData = "a cat" },
            _ => Task.FromResult(new AITrackedOperationResult<string> { Result = "1 image(s)" }),
            CancellationToken.None);

        // Assert
        captured.ShouldNotBeNull();
        captured["RunId"].ShouldBe("run-42");
    }

    // The user tag used to come from the audit entry, so it was missing when auditing was off.
    [Fact]
    public async Task TrackAsync_WithAuditDisabled_StillTagsTheCallsSpanWithTheUser()
    {
        // Arrange
        _auditLogOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = false });
        var userKey = Guid.NewGuid();
        var user = Mock.Of<Umbraco.Cms.Core.Models.Membership.IUser>(u => u.Key == userKey);
        var security = Mock.Of<Umbraco.Cms.Core.Security.IBackOfficeSecurity>(s => s.CurrentUser == user);
        var securityAccessor = Mock.Of<Umbraco.Cms.Core.Security.IBackOfficeSecurityAccessor>(a => a.BackOfficeSecurity == security);
        var tracker = CreateTracker(securityAccessor);

        // Act
        var span = await TrackModelCallAsync(tracker);

        // Assert
        span.GetTagItem(Umbraco.AI.Core.Telemetry.AITelemetry.Tags.UserId).ShouldBe(userKey.ToString());
    }

    /// <summary>
    /// Tracks a call whose work is a chat call through the OpenTelemetry middleware, as the real pipeline
    /// does, and returns the gen_ai span that call ran in. <paramref name="beforeModelCall"/> runs inside the
    /// tracked call but outside its span, where the guardrail middleware makes its nested calls.
    /// </summary>
    private static async Task<System.Diagnostics.Activity> TrackModelCallAsync(
        AIOperationTracker tracker,
        Func<Task>? beforeModelCall = null)
    {
        System.Diagnostics.Activity? span = null;
        var inner = new Mock<IChatClient>();
        inner
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback(() => span = System.Diagnostics.Activity.Current)
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        var client = new Umbraco.AI.Core.Chat.Middleware.AIOpenTelemetryChatMiddleware(
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).Apply(inner.Object);

        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == Umbraco.AI.Core.Telemetry.AITelemetry.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        await tracker.TrackAsync(
            CreateDescriptor(),
            async ct =>
            {
                if (beforeModelCall is not null)
                {
                    await beforeModelCall();
                }

                var response = await client.GetResponseAsync("hi", cancellationToken: ct);
                return new AITrackedOperationResult<string> { Result = response.Text };
            },
            CancellationToken.None);

        span.ShouldNotBeNull();
        span.Source.Name.ShouldBe(Umbraco.AI.Core.Telemetry.AITelemetry.SourceName);
        return span;
    }

    private AIOperationTracker CreateTracker(Umbraco.Cms.Core.Security.IBackOfficeSecurityAccessor? securityAccessor = null) => new(
        _contextAccessorMock.Object,
        TestOperationRecorders.Default(_auditLogServiceMock.Object, _auditLogFactoryMock.Object, _auditLogOptionsMock.Object, _usageRecordingServiceMock.Object, _usageRecordFactoryMock.Object, _analyticsOptionsMock.Object, securityAccessor),
        NullLogger<AIOperationTracker>.Instance);

    private static AIOperationDescriptor CreateDescriptor() => new()
    {
        Capability = AICapability.Chat,
        PromptData = "prompt data",
    };

    private static AIUsageRecord BuildUsageRecord(AIUsageRecordContext ctx, AIUsageRecordResult result) => new()
    {
        Id = Guid.NewGuid(),
        Timestamp = DateTime.UtcNow,
        Capability = ctx.Capability,
        ProfileId = ctx.ProfileId,
        ProfileAlias = ctx.ProfileAlias,
        ProviderId = ctx.ProviderId,
        ModelId = ctx.ModelId,
        FeatureType = ctx.FeatureType,
        FeatureId = ctx.FeatureId,
        EntityId = ctx.EntityId,
        EntityType = ctx.EntityType,
        InputTokens = result.Usage?.InputTokenCount ?? 0,
        OutputTokens = result.Usage?.OutputTokenCount ?? 0,
        TotalTokens = result.Usage?.TotalTokenCount ?? 0,
        DurationMs = result.DurationMs,
        Status = result.Succeeded ? AIUsageRecordStatus.Succeeded : AIUsageRecordStatus.Failed,
        ErrorMessage = result.ErrorMessage,
        CreatedAt = DateTime.UtcNow,
    };

    /// <summary>
    /// Wires the usage-recording mock to signal a <see cref="TaskCompletionSource{T}"/> when
    /// <c>QueueRecordUsageAsync</c> is invoked, so tests can deterministically await the
    /// fire-and-forget usage recording performed by the tracker instead of racing it.
    /// </summary>
    private TaskCompletionSource<AIUsageRecord> ArrangeUsageRecordingSignal()
    {
        var tcs = new TaskCompletionSource<AIUsageRecord>(TaskCreationOptions.RunContinuationsAsynchronously);
        _usageRecordingServiceMock
            .Setup(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()))
            .Callback<AIUsageRecord, CancellationToken>((record, _) => tcs.TrySetResult(record))
            .Returns(ValueTask.CompletedTask);
        return tcs;
    }

    private static async Task<AIUsageRecord> AwaitOrTimeout(Task<AIUsageRecord> task, int timeoutMs = 2000)
    {
        var winner = await Task.WhenAny(task, Task.Delay(timeoutMs));
        winner.ShouldBe(task, "Timed out waiting for the fire-and-forget usage record to be queued.");
        return await task;
    }

    /// <summary>
    /// Gives the fire-and-forget usage-recording task a brief window to run, then callers assert
    /// it was never invoked. The early-exit branches (disabled options / null usage) return before
    /// any await point, so this is not actually racy — the delay is defensive.
    /// </summary>
    private static Task EnsureNoUsageRecorded() => Task.Delay(100);
}

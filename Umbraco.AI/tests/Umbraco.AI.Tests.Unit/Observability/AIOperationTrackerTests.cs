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
        var expectedResponse = new AIAuditResponse { Data = "ok" };

        // Act
        var result = await tracker.TrackAsync(
            descriptor,
            _ => Task.FromResult(new AITrackedOperationResult<string>
            {
                Result = "success",
                Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5, TotalTokenCount = 15 },
                AuditResponse = expectedResponse,
            }),
            CancellationToken.None);

        await AwaitOrTimeout(usageSignal.Task);

        // Assert
        result.Result.ShouldBe("success");
        _auditLogServiceMock.Verify(x => x.QueueStartAuditLogAsync(_auditLog, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(_auditLog, It.IsAny<AIAuditPrompt?>(), expectedResponse, It.IsAny<CancellationToken>()), Times.Once);
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test 2: exception path queues audit failure + a failed usage record, then rethrows.
    // Uses RecordUsageWhenEmpty=true (image/STT-style) because the failure path always reports
    // null Usage — a capability with RecordUsageWhenEmpty=false (chat/embedding) would legitimately
    // skip the usage record on failure too, which is covered separately by Test 5.
    [Fact]
    public async Task TrackAsync_OnException_QueuesAuditFailure_AndFailedUsage_AndRethrows()
    {
        // Arrange
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor(recordUsageWhenEmpty: true);
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
        record.Status.ShouldBe("Failed");
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

    // Test 5: RecordUsageWhenEmpty=false + null Usage => NO usage record queued.
    [Fact]
    public async Task TrackAsync_NullUsage_WithRecordWhenEmptyFalse_SkipsUsage()
    {
        // Arrange
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor(recordUsageWhenEmpty: false);

        // Act
        await tracker.TrackAsync(
            descriptor,
            _ => Task.FromResult(new AITrackedOperationResult<string> { Result = "success", Usage = null }),
            CancellationToken.None);

        await EnsureNoUsageRecorded();

        // Assert
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Test 6: RecordUsageWhenEmpty=true + null Usage => usage record queued (duration only).
    [Fact]
    public async Task TrackAsync_NullUsage_WithRecordWhenEmptyTrue_RecordsUsage()
    {
        // Arrange
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor(recordUsageWhenEmpty: true);
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

    // Test 9: BeginAsync + CompleteAsync mirrors TrackAsync success behavior.
    [Fact]
    public async Task BeginThenComplete_QueuesStartCompleteAudit_AndUsage()
    {
        // Arrange
        var tracker = CreateTracker();
        var descriptor = CreateDescriptor();
        var usageSignal = ArrangeUsageRecordingSignal();
        var response = new AIAuditResponse { Data = "streamed result" };
        var usage = new UsageDetails { InputTokenCount = 3, OutputTokenCount = 7, TotalTokenCount = 10 };

        // Act
        var scope = await tracker.BeginAsync(descriptor, CancellationToken.None);
        await scope.CompleteAsync(usage, response);
        scope.Dispose();

        await AwaitOrTimeout(usageSignal.Task);

        // Assert
        _auditLogServiceMock.Verify(x => x.QueueStartAuditLogAsync(_auditLog, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(_auditLog, It.IsAny<AIAuditPrompt?>(), response, It.IsAny<CancellationToken>()), Times.Once);
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

    private AIOperationTracker CreateTracker() => new(
        _contextAccessorMock.Object,
        _auditLogServiceMock.Object,
        _auditLogFactoryMock.Object,
        _auditLogOptionsMock.Object,
        _usageRecordingServiceMock.Object,
        _usageRecordFactoryMock.Object,
        _analyticsOptionsMock.Object,
        NullLogger<AIOperationTracker>.Instance);

    private static AIOperationDescriptor CreateDescriptor(bool recordUsageWhenEmpty = false) => new()
    {
        Capability = AICapability.Chat,
        PromptData = "prompt data",
        RecordUsageWhenEmpty = recordUsageWhenEmpty,
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
        Status = result.Succeeded ? "Succeeded" : "Failed",
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

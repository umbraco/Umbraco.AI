using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

/// <summary>
/// Tests for <see cref="AITrackingEmbeddingGenerator"/>, the single tracker-backed generator that
/// replaced the former AITrackingEmbeddingGenerator / AIUsageRecordingEmbeddingGenerator /
/// AIAuditingEmbeddingGenerator trio. Uses a real <see cref="AIOperationTracker"/> wired with
/// mocked audit/usage collaborators (mirroring <c>AIOperationTrackerTests</c>) so behavior is
/// verified end-to-end through the tracker, rather than mocking the (internal,
/// non-mockable-return-type) tracker contract itself.
/// </summary>
public class AITrackingEmbeddingGeneratorTests
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

    public AITrackingEmbeddingGeneratorTests()
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
        _runtimeContext.SetValue(Constants.ContextKeys.ModelId, "embedding-test");
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

    #region GenerateAsync

    [Fact]
    public async Task GenerateAsync_OnSuccess_QueuesCompleteAuditWithEmbeddingsAndUsage()
    {
        // Arrange
        var usage = new UsageDetails { InputTokenCount = 8, TotalTokenCount = 8 };
        var fakeGenerator = new FakeEmbeddingGenerator((values, _, _) =>
        {
            var embeddings = values.Select(_ => new Embedding<float>(new[] { 0.1f, 0.2f, 0.3f })).ToList();
            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings) { Usage = usage });
        });
        var generator = CreateGenerator(fakeGenerator);
        var usageSignal = ArrangeUsageRecordingSignal();

        // Act
        var result = await generator.GenerateAsync(["hello", "world"]);

        var record = await AwaitOrTimeout(usageSignal.Task);

        // Assert
        result.Count.ShouldBe(2);

        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.Is<AIAuditResponse?>(r =>
                r != null &&
                ReferenceEquals(r.Data, result) &&
                r.Usage == usage),
            CancellationToken.None), Times.Once);

        record.InputTokens.ShouldBe(8);
        record.TotalTokens.ShouldBe(8);
    }

    [Fact]
    public async Task GenerateAsync_OnException_QueuesFailureWithNoneToken_AndRethrows()
    {
        // Arrange — inner generator throws regardless of cancellation token state
        var fakeGenerator = new FakeEmbeddingGenerator((_, _, _) =>
            Task.FromException<GeneratedEmbeddings<Embedding<float>>>(new InvalidOperationException("AI error")));
        var generator = CreateGenerator(fakeGenerator);

        // Use an already-cancelled token to simulate client disconnection
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            generator.GenerateAsync(["hello"], cancellationToken: cts.Token));

        // The failure must be recorded with CancellationToken.None so it isn't dropped when the
        // request token is cancelled (the original cause of entries being stuck in "Running")
        _auditLogServiceMock.Verify(x => x.QueueRecordAuditLogFailureAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.IsAny<Exception>(),
            CancellationToken.None), Times.Once);
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_NullUsage_SkipsUsageRecording()
    {
        // Arrange — Embedding uses RecordUsageWhenEmpty=false, so a null Usage must not queue a record.
        var fakeGenerator = new FakeEmbeddingGenerator();
        var generator = CreateGenerator(fakeGenerator);

        // Act
        await generator.GenerateAsync(["hello"]);
        await EnsureNoUsageRecorded();

        // Assert
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        // Audit still completes even when there's no usage to record.
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog, It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_ExtractsMetadataFromRuntimeContextLogKeys()
    {
        // Arrange — LogKeys declared in the runtime context must flow through to audit metadata,
        // mirroring chat/STT. (Previously embedding incorrectly read LogKeys from
        // EmbeddingGenerationOptions.AdditionalProperties, a location no caller populates.)
        _runtimeContext.SetValue(Constants.ContextKeys.LogKeys, new[] { "customKey" });
        _runtimeContext.SetValue("customKey", "customValue");

        var fakeGenerator = new FakeEmbeddingGenerator();
        var generator = CreateGenerator(fakeGenerator);

        // Act
        await generator.GenerateAsync(["hello"]);

        // Assert
        _auditLogFactoryMock.Verify(x => x.Create(
            It.IsAny<AIAuditContext>(),
            It.Is<IReadOnlyDictionary<string, string>?>(m => m != null && m["customKey"] == "customValue"),
            It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithoutLogKeysInRuntimeContext_PassesNullMetadata()
    {
        // Arrange
        var fakeGenerator = new FakeEmbeddingGenerator();
        var generator = CreateGenerator(fakeGenerator);

        // Act
        await generator.GenerateAsync(["hello"]);

        // Assert
        _auditLogFactoryMock.Verify(x => x.Create(
            It.IsAny<AIAuditContext>(),
            null,
            It.IsAny<Guid?>()), Times.Once);
    }

    #endregion

    #region GetService

    [Fact]
    public void GetService_ReturnsTrackingGenerator()
    {
        // Arrange
        var fakeGenerator = new FakeEmbeddingGenerator();
        var generator = CreateGenerator(fakeGenerator);

        // Act
        var service = generator.GetService<AITrackingEmbeddingGenerator>();

        // Assert
        service.ShouldBe(generator);
    }

    #endregion

    private AITrackingEmbeddingGenerator CreateGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator) =>
        new(innerGenerator, CreateTracker(), _contextAccessorMock.Object);

    private AIOperationTracker CreateTracker() => new(
        _contextAccessorMock.Object,
        _auditLogServiceMock.Object,
        _auditLogFactoryMock.Object,
        _auditLogOptionsMock.Object,
        _usageRecordingServiceMock.Object,
        _usageRecordFactoryMock.Object,
        _analyticsOptionsMock.Object,
        NullLogger<AIOperationTracker>.Instance);

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
    /// it was never invoked.
    /// </summary>
    private static Task EnsureNoUsageRecorded() => Task.Delay(100);
}

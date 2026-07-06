using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.SpeechToText;
using Umbraco.AI.Tests.Common.Fakes;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Tests.Unit.Middleware;

/// <summary>
/// Tests for <see cref="AITrackingSpeechToTextClient"/>, the single tracker-backed client that
/// replaced the former AITrackingSpeechToTextClient / AIUsageRecordingSpeechToTextClient /
/// AIAuditingSpeechToTextClient trio. Uses a real <see cref="AIOperationTracker"/> wired with
/// mocked audit/usage collaborators (mirroring <c>AIOperationTrackerTests</c>) so behavior is
/// verified end-to-end through the tracker, rather than mocking the (internal,
/// non-mockable-return-type) tracker contract itself.
/// </summary>
public class AITrackingSpeechToTextClientTests
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

    public AITrackingSpeechToTextClientTests()
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
        _runtimeContext.SetValue(Constants.ContextKeys.ModelId, "stt-test");
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

    #region GetTextAsync

    [Fact]
    public async Task GetTextAsync_OnSuccess_QueuesCompleteAuditWithTranscriptionTextAndNoUsage()
    {
        // Arrange
        var fakeClient = new FakeSpeechToTextClient("Hello, world!");
        var client = CreateClient(fakeClient);

        // Act
        var response = await client.GetTextAsync(new MemoryStream());

        // Assert
        response.Text.ShouldBe("Hello, world!");

        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.Is<AIAuditResponse?>(r =>
                r != null &&
                (string?)r.Data == "Hello, world!" &&
                r.Usage == null),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetTextAsync_OnSuccess_RecordsUsageEvenWithoutUsageDetails()
    {
        // Arrange — STT uses RecordUsageWhenEmpty=true, so a duration/status record is queued
        // even though there is no UsageDetails to report (STT has no token usage).
        var fakeClient = new FakeSpeechToTextClient();
        var client = CreateClient(fakeClient);
        var usageSignal = ArrangeUsageRecordingSignal();

        // Act
        await client.GetTextAsync(new MemoryStream());
        var record = await AwaitOrTimeout(usageSignal.Task);

        // Assert
        record.InputTokens.ShouldBe(0);
        record.OutputTokens.ShouldBe(0);
        record.TotalTokens.ShouldBe(0);
        record.Status.ShouldBe("Succeeded");
    }

    [Fact]
    public async Task GetTextAsync_OnException_QueuesFailureWithNoneToken_RethrowsAndRecordsFailedUsage()
    {
        // Arrange — inner client throws regardless of cancellation token state
        var throwingClient = new ThrowingSpeechToTextClient(new InvalidOperationException("AI error"));
        var client = CreateClient(throwingClient);
        var usageSignal = ArrangeUsageRecordingSignal();

        // Use an already-cancelled token to simulate client disconnection
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            client.GetTextAsync(new MemoryStream(), cancellationToken: cts.Token));

        // The failure must be recorded with CancellationToken.None so it isn't dropped when the
        // request token is cancelled (the original cause of entries being stuck in "Running")
        _auditLogServiceMock.Verify(x => x.QueueRecordAuditLogFailureAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.IsAny<Exception>(),
            CancellationToken.None), Times.Once);
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()), Times.Never);

        // RecordUsageWhenEmpty=true means even a failed operation with no usage records duration/status.
        var record = await AwaitOrTimeout(usageSignal.Task);
        record.Status.ShouldBe("Failed");
        record.ErrorMessage.ShouldBe("AI error");
    }

    [Fact]
    public async Task GetTextAsync_ExtractsMetadataFromRuntimeContextLogKeys()
    {
        // Arrange — LogKeys declared in the runtime context must flow through to audit metadata.
        _runtimeContext.SetValue(Constants.ContextKeys.LogKeys, new[] { "customKey" });
        _runtimeContext.SetValue("customKey", "customValue");

        var fakeClient = new FakeSpeechToTextClient();
        var client = CreateClient(fakeClient);

        // Act
        await client.GetTextAsync(new MemoryStream());

        // Assert
        _auditLogFactoryMock.Verify(x => x.Create(
            It.IsAny<AIAuditContext>(),
            It.Is<IReadOnlyDictionary<string, string>?>(m => m != null && m["customKey"] == "customValue"),
            It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task GetTextAsync_WithOptions_BuildsPromptDataFromModelAndLanguage()
    {
        // Arrange — STT has no text prompt, so BuildPromptData captures the options metadata instead.
        var fakeClient = new FakeSpeechToTextClient();
        var client = CreateClient(fakeClient);
        var options = new SpeechToTextOptions { ModelId = "whisper-1", SpeechLanguage = "en" };

        // Act
        await client.GetTextAsync(new MemoryStream(), options);

        // Assert
        _auditLogFactoryMock.Verify(x => x.Create(
            It.Is<AIAuditContext>(c => PromptHasModelAndLanguage(c.Prompt, "whisper-1", "en")),
            It.IsAny<IReadOnlyDictionary<string, string>?>(),
            It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task GetTextAsync_WithoutOptions_BuildsFallbackPromptData()
    {
        // Arrange
        var fakeClient = new FakeSpeechToTextClient();
        var client = CreateClient(fakeClient);

        // Act
        await client.GetTextAsync(new MemoryStream());

        // Assert
        _auditLogFactoryMock.Verify(x => x.Create(
            It.Is<AIAuditContext>(c => (string?)c.Prompt == "speech-to-text transcription"),
            It.IsAny<IReadOnlyDictionary<string, string>?>(),
            It.IsAny<Guid?>()), Times.Once);
    }

    #endregion

    #region GetStreamingTextAsync

    [Fact]
    public async Task GetStreamingTextAsync_YieldsImmediately_ThenAggregatesTextAndCompletesAuditWithNoUsage()
    {
        // Arrange
        var fakeClient = new FakeStreamingSpeechToTextClient("Hello ", "world");
        var client = CreateClient(fakeClient);

        // Act
        var updates = new List<SpeechToTextResponseUpdate>();
        await foreach (var update in client.GetStreamingTextAsync(new MemoryStream()))
        {
            updates.Add(update);
        }

        // Assert — updates were yielded (not buffered until the end)
        updates.Count.ShouldBe(2);

        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.Is<AIAuditResponse?>(r =>
                r != null &&
                (string?)r.Data == "Hello world" &&
                r.Usage == null),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetStreamingTextAsync_OnSuccess_RecordsUsageEvenWithoutUsageDetails()
    {
        // Arrange — STT uses RecordUsageWhenEmpty=true even for the streaming path.
        var fakeClient = new FakeStreamingSpeechToTextClient("Hello");
        var client = CreateClient(fakeClient);
        var usageSignal = ArrangeUsageRecordingSignal();

        // Act
        await foreach (var _ in client.GetStreamingTextAsync(new MemoryStream()))
        {
        }

        var record = await AwaitOrTimeout(usageSignal.Task);

        // Assert
        record.Status.ShouldBe("Succeeded");
        record.TotalTokens.ShouldBe(0);
    }

    [Fact]
    public async Task GetStreamingTextAsync_OnMidStreamException_QueuesFailureWithNoneToken_AndRethrows()
    {
        // Arrange — inner client throws during streaming, after yielding some updates
        var throwingClient = new ThrowingStreamingSpeechToTextClient(new HttpRequestException("Connection reset"), "partial ");
        var client = CreateClient(throwingClient);
        var updates = new List<SpeechToTextResponseUpdate>();

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await foreach (var update in client.GetStreamingTextAsync(new MemoryStream()))
            {
                updates.Add(update);
            }
        });

        // The partial update was still yielded to the caller before the failure.
        updates.Count.ShouldBe(1);

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

    #endregion

    #region GetService

    [Fact]
    public void GetService_ReturnsTrackingClient()
    {
        // Arrange
        var fakeClient = new FakeSpeechToTextClient();
        var client = CreateClient(fakeClient);

        // Act
        var service = client.GetService<AITrackingSpeechToTextClient>();

        // Assert
        service.ShouldBe(client);
    }

    #endregion

    private AITrackingSpeechToTextClient CreateClient(ISpeechToTextClient innerClient) =>
        new(innerClient, CreateTracker(), _contextAccessorMock.Object);

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
    /// Reflects into the anonymous <c>{ Type, ModelId, SpeechLanguage }</c> object produced by
    /// <c>AITrackingSpeechToTextClient.BuildPromptData</c> to verify its shape without exposing it.
    /// </summary>
    private static bool PromptHasModelAndLanguage(object? prompt, string modelId, string language)
    {
        if (prompt is null)
        {
            return false;
        }

        var type = prompt.GetType();
        var modelValue = type.GetProperty("ModelId")?.GetValue(prompt) as string;
        var languageValue = type.GetProperty("SpeechLanguage")?.GetValue(prompt) as string;
        return modelValue == modelId && languageValue == language;
    }

    /// <summary>
    /// A speech-to-text client whose <see cref="GetTextAsync"/> throws immediately.
    /// </summary>
    private sealed class ThrowingSpeechToTextClient : ISpeechToTextClient
    {
        private readonly Exception _exception;

        public ThrowingSpeechToTextClient(Exception exception) => _exception = exception;

        public Task<SpeechToTextResponse> GetTextAsync(Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromException<SpeechToTextResponse>(_exception);

        public IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public SpeechToTextClientMetadata Metadata => new("ThrowingClient", null, null);
        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }

    /// <summary>
    /// A speech-to-text client that streams one <see cref="SpeechToTextResponseUpdate"/> per
    /// supplied text part.
    /// </summary>
    private sealed class FakeStreamingSpeechToTextClient : ISpeechToTextClient
    {
        private readonly string[] _parts;

        public FakeStreamingSpeechToTextClient(params string[] parts) => _parts = parts;

        public Task<SpeechToTextResponse> GetTextAsync(Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
            Stream audioSpeechStream,
            SpeechToTextOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var part in _parts)
            {
                await Task.Yield();
                yield return new SpeechToTextResponseUpdate(part);
            }
        }

        public SpeechToTextClientMetadata Metadata => new("FakeStreamingClient", null, null);
        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }

    /// <summary>
    /// A speech-to-text client whose streaming implementation yields any supplied text parts and
    /// then throws. Used to simulate connection resets and similar mid-stream errors.
    /// </summary>
    private sealed class ThrowingStreamingSpeechToTextClient : ISpeechToTextClient
    {
        private readonly Exception _exception;
        private readonly string[] _partsBeforeFailure;

        public ThrowingStreamingSpeechToTextClient(Exception exception, params string[] partsBeforeFailure)
        {
            _exception = exception;
            _partsBeforeFailure = partsBeforeFailure;
        }

        public Task<SpeechToTextResponse> GetTextAsync(Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromException<SpeechToTextResponse>(_exception);

        public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
            Stream audioSpeechStream,
            SpeechToTextOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var part in _partsBeforeFailure)
            {
                await Task.Yield();
                yield return new SpeechToTextResponseUpdate(part);
            }

            await Task.Yield();
            throw _exception;
#pragma warning disable CS0162 // Unreachable code - required to satisfy IAsyncEnumerable<T> return type
            yield break;
#pragma warning restore CS0162
        }

        public SpeechToTextClientMetadata Metadata => new("ThrowingStreamingClient", null, null);
        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }
}

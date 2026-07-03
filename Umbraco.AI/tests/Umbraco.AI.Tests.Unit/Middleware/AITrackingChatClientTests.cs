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
/// Tests for <see cref="AITrackingChatClient"/>, the single tracker-backed client that replaced
/// the former AITrackingChatClient / AIUsageRecordingChatClient / AIAuditingChatClient trio.
/// Uses a real <see cref="AIOperationTracker"/> wired with mocked audit/usage collaborators
/// (mirroring <c>AIOperationTrackerTests</c>) so behavior is verified end-to-end through the
/// tracker, rather than mocking the (internal, non-mockable-return-type) tracker contract itself.
/// </summary>
public class AITrackingChatClientTests
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

    public AITrackingChatClientTests()
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

    #region GetResponseAsync

    [Fact]
    public async Task GetResponseAsync_OnSuccess_QueuesCompleteAuditWithMessagesAndUsage()
    {
        // Arrange
        var responseMessage = new ChatMessage(ChatRole.Assistant, "Hello, world!");
        var usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5, TotalTokenCount = 15 };
        var fakeClient = new FakeChatClient((_, _, _) =>
            Task.FromResult(new ChatResponse(responseMessage) { Usage = usage }));
        var client = CreateClient(fakeClient);
        var usageSignal = ArrangeUsageRecordingSignal();

        // Act
        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Hi")]);

        var record = await AwaitOrTimeout(usageSignal.Task);

        // Assert
        response.Messages[0].Text.ShouldBe("Hello, world!");

        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.Is<AIAuditResponse?>(r =>
                r != null &&
                r.Usage == usage &&
                ((IReadOnlyList<ChatMessage>)r.Data!).Count == 1 &&
                ((IReadOnlyList<ChatMessage>)r.Data!)[0].Text == "Hello, world!"),
            CancellationToken.None), Times.Once);

        record.InputTokens.ShouldBe(10);
        record.OutputTokens.ShouldBe(5);
        record.TotalTokens.ShouldBe(15);
    }

    [Fact]
    public async Task GetResponseAsync_WithToolCalls_AuditsAllResponseMessages()
    {
        // Arrange — audit response.Messages must include tool-call content for agentic scenarios.
        var functionCall = new FunctionCallContent(
            callId: "tc_001",
            name: "get_weather",
            arguments: new Dictionary<string, object?> { ["city"] = "London" });

        var responseMessage = new ChatMessage(ChatRole.Assistant, new List<AIContent>
        {
            new TextContent("Let me check the weather."),
            functionCall
        });

        var fakeClient = new FakeChatClient((_, _, _) =>
            Task.FromResult(new ChatResponse(responseMessage)));
        var client = CreateClient(fakeClient);

        // Act
        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "What's the weather?")]);

        // Assert
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.Is<AIAuditResponse?>(r =>
                r != null &&
                ((IReadOnlyList<ChatMessage>)r.Data!)[0].Contents.OfType<FunctionCallContent>().Any(c => c.Name == "get_weather")),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_OnException_QueuesFailureWithNoneToken_AndRethrows()
    {
        // Arrange — inner client throws regardless of cancellation token state
        var innerClient = new FakeChatClient((_, _, _) => Task.FromException<ChatResponse>(new InvalidOperationException("AI error")));
        var client = CreateClient(innerClient);

        // Use an already-cancelled token to simulate client disconnection
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], cancellationToken: cts.Token));

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
    public async Task GetResponseAsync_NullUsage_SkipsUsageRecording()
    {
        // Arrange — Chat uses RecordUsageWhenEmpty=false, so a null Usage must not queue a record.
        var responseMessage = new ChatMessage(ChatRole.Assistant, "Response");
        var fakeClient = new FakeChatClient((_, _, _) =>
            Task.FromResult(new ChatResponse(responseMessage)));
        var client = CreateClient(fakeClient);

        // Act
        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Hi")]);
        await EnsureNoUsageRecorded();

        // Assert
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        // Audit still completes even when there's no usage to record.
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog, It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_ExtractsMetadataFromRuntimeContextLogKeys()
    {
        // Arrange — LogKeys declared in the runtime context must flow through to audit metadata.
        _runtimeContext.SetValue(Constants.ContextKeys.LogKeys, new[] { "customKey" });
        _runtimeContext.SetValue("customKey", "customValue");

        var fakeClient = new FakeChatClient("response");
        var client = CreateClient(fakeClient);

        // Act
        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Hi")]);

        // Assert
        _auditLogFactoryMock.Verify(x => x.Create(
            It.IsAny<AIAuditContext>(),
            It.Is<IReadOnlyDictionary<string, string>?>(m => m != null && m["customKey"] == "customValue"),
            It.IsAny<Guid?>()), Times.Once);
    }

    #endregion

    #region GetStreamingResponseAsync

    [Fact]
    public async Task GetStreamingResponseAsync_YieldsImmediately_ThenAggregatesAndCompletesAudit()
    {
        // Arrange
        var fakeClient = new FakeChatClient("Hello world");
        var client = CreateClient(fakeClient);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "Hi")]))
        {
            updates.Add(update);
        }

        // Assert — updates were yielded (not buffered until the end)
        updates.Count.ShouldBeGreaterThan(1);

        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.Is<AIAuditResponse?>(r =>
                r != null &&
                ((IReadOnlyList<ChatMessage>)r.Data!)[0].Text.Contains("Hello")),
            CancellationToken.None), Times.Once);

        // FakeChatClient's streaming updates carry no UsageContent, so the aggregated Usage is null;
        // Chat's RecordUsageWhenEmpty=false means no usage record should be queued.
        await EnsureNoUsageRecorded();
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_OnMidStreamException_QueuesFailureWithNoneToken_AndRethrows()
    {
        // Arrange — inner client throws during streaming
        var throwingClient = new ThrowingStreamingChatClient(new HttpRequestException("Connection reset"));
        var client = CreateClient(throwingClient);

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
            }
        });

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
        var fakeClient = new FakeChatClient();
        var client = CreateClient(fakeClient);

        // Act
        var service = client.GetService<AITrackingChatClient>();

        // Assert
        service.ShouldBe(client);
    }

    #endregion

    private AITrackingChatClient CreateClient(IChatClient innerClient) =>
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
    /// Gives the fire-and-forget usage-recording task a brief window to run, then callers assert
    /// it was never invoked.
    /// </summary>
    private static Task EnsureNoUsageRecorded() => Task.Delay(100);

    /// <summary>
    /// A chat client whose streaming implementation throws on the first MoveNextAsync call.
    /// Used to simulate connection resets and similar mid-stream errors.
    /// </summary>
    private sealed class ThrowingStreamingChatClient : IChatClient
    {
        private readonly Exception _exception;

        public ThrowingStreamingChatClient(Exception exception) => _exception = exception;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromException<ChatResponse>(_exception);

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw _exception;
#pragma warning disable CS0162 // Unreachable code - required to satisfy IAsyncEnumerable<T> return type
            yield break;
#pragma warning restore CS0162
        }

        public ChatClientMetadata Metadata => new("ThrowingClient", null, null);
        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }
}

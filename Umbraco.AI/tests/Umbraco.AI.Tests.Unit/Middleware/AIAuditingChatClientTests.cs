using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.AuditLog.Middleware;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

public class AIAuditingChatClientTests
{
    private readonly Mock<IAIRuntimeContextAccessor> _contextAccessorMock;
    private readonly Mock<IAIAuditLogService> _auditLogServiceMock;
    private readonly Mock<IAIAuditLogFactory> _auditLogFactoryMock;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;
    private readonly AIAuditLog _auditLog;

    public AIAuditingChatClientTests()
    {
        _contextAccessorMock = new Mock<IAIRuntimeContextAccessor>();
        _auditLogServiceMock = new Mock<IAIAuditLogService>();
        _auditLogFactoryMock = new Mock<IAIAuditLogFactory>();

        var optionsMock = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        optionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = true });
        _auditLogOptions = optionsMock.Object;

        _auditLog = new AIAuditLog { Id = Guid.NewGuid() };
        _auditLogFactoryMock
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Returns(_auditLog);

        // Enable auditing by returning a non-null runtime context
        var runtimeContext = new AIRuntimeContext([]);
        _contextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);

        _auditLogServiceMock
            .Setup(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        _auditLogServiceMock
            .Setup(x => x.QueueCompleteAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        _auditLogServiceMock
            .Setup(x => x.QueueRecordAuditLogFailureAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
    }

    [Fact]
    public async Task GetResponseAsync_OnSuccess_QueuesCompletionWithNoneToken()
    {
        // Arrange
        var client = CreateClient(new FakeChatClient("response"));

        // Act
        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]);

        // Assert — status update must use CancellationToken.None so it isn't skipped on disconnects
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.IsAny<AIAuditResponse?>(),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetResponseAsync_OnException_QueuesFailureWithNoneToken()
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
    }

    [Fact]
    public async Task GetStreamingResponseAsync_OnSuccess_QueuesCompletionWithNoneToken()
    {
        // Arrange
        var client = CreateClient(new FakeChatClient("hello world"));

        // Act — consume the stream fully
        await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
        }

        // Assert
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.IsAny<AIAuditResponse?>(),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_OnException_QueuesFailureWithNoneToken()
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
    }

    private AIAuditingChatClient CreateClient(IChatClient innerClient) =>
        new(innerClient, _contextAccessorMock.Object, _auditLogServiceMock.Object,
            _auditLogFactoryMock.Object, _auditLogOptions);

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
            CancellationToken cancellationToken = default)
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

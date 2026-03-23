using Microsoft.Extensions.AI;
using Umbraco.AI.Core.FileProcessing;

namespace Umbraco.AI.Tests.Unit.FileProcessing;

public class AIFileProcessingChatClientTests
{
    #region GetResponseAsync

    [Fact]
    public async Task GetResponseAsync_WithHandledDataContent_ReplacesWithTextContent()
    {
        // Arrange
        var handler = new FakeHandler("text/csv", "extracted,data");
        var (client, inner) = CreateClient(handler);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, [CreateDataContent(new byte[] { 1, 2, 3 }, "text/csv", "report.csv")]),
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert
        var sentMessages = inner.LastMessages!;
        sentMessages.Count.ShouldBe(1);
        var contents = sentMessages[0].Contents;
        contents.Count.ShouldBe(1);
        contents[0].ShouldBeOfType<TextContent>();
        ((TextContent)contents[0]).Text.ShouldContain("[File: report.csv]");
        ((TextContent)contents[0]).Text.ShouldContain("extracted,data");
    }

    [Fact]
    public async Task GetResponseAsync_WithUnhandledDataContent_PassesThrough()
    {
        // Arrange — handler only handles text/csv, not image/png
        var handler = new FakeHandler("text/csv", "ignored");
        var (client, inner) = CreateClient(handler);

        var imageData = new DataContent(new byte[] { 1, 2, 3 }, "image/png");
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, [imageData]),
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert
        var sentMessages = inner.LastMessages!;
        sentMessages[0].Contents[0].ShouldBeOfType<DataContent>();
    }

    [Fact]
    public async Task GetResponseAsync_WithMixedContent_OnlyConvertsHandledTypes()
    {
        // Arrange
        var handler = new FakeHandler("text/csv", "csv content");
        var (client, inner) = CreateClient(handler);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User,
            [
                new TextContent("Check these files"),
                CreateDataContent(new byte[] { 1 }, "text/csv", "data.csv"),
                new DataContent(new byte[] { 2 }, "image/png"),
            ]),
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert
        var contents = inner.LastMessages![0].Contents;
        contents.Count.ShouldBe(3);
        contents[0].ShouldBeOfType<TextContent>(); // original text
        contents[1].ShouldBeOfType<TextContent>(); // converted CSV
        contents[2].ShouldBeOfType<DataContent>(); // untouched image
    }

    [Fact]
    public async Task GetResponseAsync_WithNoDataContent_PassesMessagesUnchanged()
    {
        // Arrange
        var handler = new FakeHandler("text/csv", "ignored");
        var (client, inner) = CreateClient(handler);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Just text"),
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert
        inner.LastMessages![0].Text.ShouldBe("Just text");
    }

    [Fact]
    public async Task GetResponseAsync_WithMultipleHandlers_FirstMatchWins()
    {
        // Arrange
        var handler1 = new FakeHandler("text/csv", "handler1 output");
        var handler2 = new FakeHandler("text/csv", "handler2 output");
        var (client, inner) = CreateClient(handler1, handler2);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, [CreateDataContent(new byte[] { 1 }, "text/csv", "data.csv")]),
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert
        var text = ((TextContent)inner.LastMessages![0].Contents[0]).Text;
        text.ShouldContain("handler1 output");
        text.ShouldNotContain("handler2 output");
    }

    [Fact]
    public async Task GetResponseAsync_PreservesMessageRoleAndProperties()
    {
        // Arrange
        var handler = new FakeHandler("text/csv", "data");
        var (client, inner) = CreateClient(handler);

        var message = new ChatMessage(ChatRole.User, [new DataContent(new byte[] { 1 }, "text/csv")])
        {
            AuthorName = "TestUser",
            AdditionalProperties = new AdditionalPropertiesDictionary { ["custom"] = "value" },
        };

        // Act
        await client.GetResponseAsync([message]);

        // Assert
        var sent = inner.LastMessages![0];
        sent.Role.ShouldBe(ChatRole.User);
        sent.AuthorName.ShouldBe("TestUser");
        sent.AdditionalProperties!["custom"].ShouldBe("value");
    }

    #endregion

    #region GetStreamingResponseAsync

    [Fact]
    public async Task GetStreamingResponseAsync_WithHandledDataContent_ReplacesWithTextContent()
    {
        // Arrange
        var handler = new FakeHandler("text/csv", "streamed data");
        var (client, inner) = CreateClient(handler);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, [CreateDataContent(new byte[] { 1 }, "text/csv", "data.csv")]),
        };

        // Act
        await foreach (var _ in client.GetStreamingResponseAsync(messages))
        {
            // consume
        }

        // Assert
        var contents = inner.LastMessages![0].Contents;
        contents[0].ShouldBeOfType<TextContent>();
        ((TextContent)contents[0]).Text.ShouldContain("streamed data");
    }

    #endregion

    #region Test Helpers

    private static (AIFileProcessingChatClient Client, CapturingChatClient Inner) CreateClient(
        params IAIFileProcessingHandler[] handlers)
    {
        var inner = new CapturingChatClient();
        var collection = new AIFileProcessingHandlerCollection(() => handlers);
        var client = new AIFileProcessingChatClient(inner, collection);
        return (client, inner);
    }

    private static DataContent CreateDataContent(byte[] data, string mediaType, string filename)
        => new(data, mediaType) { Name = filename };

    private sealed class FakeHandler : IAIFileProcessingHandler
    {
        private readonly string _mimeType;
        private readonly string _content;

        public FakeHandler(string mimeType, string content)
        {
            _mimeType = mimeType;
            _content = content;
        }

        public bool CanHandle(string mimeType) => string.Equals(mimeType, _mimeType, StringComparison.OrdinalIgnoreCase);

        public Task<AIFileProcessingResult> ProcessAsync(
            ReadOnlyMemory<byte> data, string mimeType, string? filename,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AIFileProcessingResult(_content, false));
    }

    private sealed class CapturingChatClient : IChatClient
    {
        public IList<ChatMessage>? LastMessages { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastMessages = chatMessages.ToList();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastMessages = chatMessages.ToList();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
            await Task.CompletedTask;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    #endregion
}

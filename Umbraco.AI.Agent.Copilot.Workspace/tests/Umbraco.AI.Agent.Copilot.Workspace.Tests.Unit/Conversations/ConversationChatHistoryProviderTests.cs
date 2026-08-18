using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Core.FileStore;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Tests for what <see cref="ConversationChatHistoryProvider"/> commits to the durable history after a run.
/// The runtime-context system message is re-injected on every run, so storing it would stack an identical
/// block per turn and replay all of them back to the model on the next one.
/// </summary>
public class ConversationChatHistoryProviderTests
{
    private static readonly Guid ConversationId = Guid.NewGuid();

    private static ConversationChatHistoryProvider CreateProvider(Mock<IAIFileStore>? fileStore = null)
        => new(Mock.Of<IAIConversationRepository>(), (fileStore ?? new Mock<IAIFileStore>()).Object);

    [Fact]
    public async Task ToStoredMessagesAsync_DropsTheInjectedSystemMessage()
    {
        // Arrange — a normal turn: the agent layer prepends the runtime context to the user's message.
        ChatMessage[] request =
        [
            new(ChatRole.System, "## Current User\n- Key: 1e70f841"),
            new(ChatRole.User, "Name three colours"),
        ];
        ChatMessage[] response = [new(ChatRole.Assistant, "Red, blue, green.")];

        // Act
        var stored = await CreateProvider().ToStoredMessagesAsync(ConversationId, request, response);

        // Assert
        stored.Select(m => m.Role).ShouldBe(["user", "assistant"]);
        stored.ShouldNotContain(m => m.Role == "system");
    }

    [Fact]
    public async Task ToStoredMessagesAsync_KeepsToolAndAssistantMessages()
    {
        // Arrange — only system messages are dropped; a tool-using turn is stored whole.
        ChatMessage[] request = [new(ChatRole.User, "What is the weather?")];
        ChatMessage[] response =
        [
            new(ChatRole.Assistant, "Checking..."),
            new(ChatRole.Tool, "{\"temp\":21}"),
            new(ChatRole.Assistant, "It is 21 degrees."),
        ];

        // Act
        var stored = await CreateProvider().ToStoredMessagesAsync(ConversationId, request, response);

        // Assert
        stored.Select(m => m.Role).ShouldBe(["user", "assistant", "tool", "assistant"]);
    }

    [Fact]
    public async Task ToStoredMessagesAsync_WithNoResponse_StillStoresTheInboundTurn()
    {
        // Arrange
        ChatMessage[] request = [new(ChatRole.User, "Hello")];

        // Act
        var stored = await CreateProvider().ToStoredMessagesAsync(ConversationId, request, null);

        // Assert
        stored.Count.ShouldBe(1);
        stored[0].ContentText.ShouldBe("Hello");
    }

    [Fact]
    public async Task ToStoredMessagesAsync_WithOnlyASystemMessage_StoresNothing()
    {
        // Arrange — a run that contributes nothing but context must not bump the conversation.
        ChatMessage[] request = [new(ChatRole.System, "## Current User")];

        // Act
        var stored = await CreateProvider().ToStoredMessagesAsync(ConversationId, request, null);

        // Assert
        stored.ShouldBeEmpty();
    }

    /// <summary>
    /// The file store is the one place attachment bytes live; ContentJson must never carry a second,
    /// independently-aging copy of them.
    /// </summary>
    public class Attachments
    {
        private static byte[] SomeBytes => "not a real image"u8.ToArray();

        [Fact]
        public async Task ToStoredMessagesAsync_AttachmentAlreadyStored_ReferencesItInsteadOfEmbeddingBytes()
        {
            // Arrange — mirrors what AGUIMessageConverter produces for a resolved upload: a DataContent
            // already tagged with the id it's stored under in the file store.
            var data = new DataContent(SomeBytes, "image/png")
            {
                AdditionalProperties = new AdditionalPropertiesDictionary { [AIFileContentMarker.FileIdPropertyKey] = "file-abc" }
            };
            ChatMessage[] request = [new(ChatRole.User, [data])];
            var fileStore = new Mock<IAIFileStore>(MockBehavior.Strict);

            // Act
            var stored = await CreateProvider(fileStore).ToStoredMessagesAsync(ConversationId, request, null);

            // Assert — StoreAsync is never called (Strict mock would fail the test if it were); the
            // existing file id is reused as-is.
            stored.Count.ShouldBe(1);
            stored[0].ContentJson.ShouldNotContain(Convert.ToBase64String(SomeBytes));
            stored[0].ContentJson.ShouldContain("file-abc");
        }

        [Fact]
        public async Task ToStoredMessagesAsync_AttachmentWithNoFileIdYet_StoresItThenReferencesIt()
        {
            // Arrange — content built outside the normal upload pipeline (no marker). The invariant
            // "ContentJson never embeds bytes" must hold even here, not just for the common path.
            var data = new DataContent(SomeBytes, "image/png");
            ChatMessage[] request = [new(ChatRole.User, [data])];
            var fileStore = new Mock<IAIFileStore>();
            fileStore
                .Setup(x => x.StoreAsync(ConversationId.ToString(), It.IsAny<byte[]>(), "image/png", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync("file-new");

            // Act
            var stored = await CreateProvider(fileStore).ToStoredMessagesAsync(ConversationId, request, null);

            // Assert
            fileStore.Verify(x => x.StoreAsync(ConversationId.ToString(), It.IsAny<byte[]>(), "image/png", null, It.IsAny<CancellationToken>()), Times.Once);
            stored[0].ContentJson.ShouldNotContain(Convert.ToBase64String(SomeBytes));
            stored[0].ContentJson.ShouldContain("file-new");
        }

        [Fact]
        public async Task ToStoredMessagesAsync_MessageWithNoAttachments_IsUnaffected()
        {
            ChatMessage[] request = [new(ChatRole.User, "just text")];
            var fileStore = new Mock<IAIFileStore>(MockBehavior.Strict);

            var stored = await CreateProvider(fileStore).ToStoredMessagesAsync(ConversationId, request, null);

            stored[0].ContentText.ShouldBe("just text");
        }

        private static UriContent Reference(string fileId)
            => new(new Uri($"urn:umbraco-ai-agent-file:{fileId}"), "image/png")
            {
                AdditionalProperties = new AdditionalPropertiesDictionary { [AIFileContentMarker.FileIdPropertyKey] = fileId }
            };

        [Fact]
        public async Task RehydrateAttachmentsAsync_ReferenceStillInFileStore_RestoresRealBytes()
        {
            var message = new ChatMessage(ChatRole.User, [Reference("file-abc")]);
            var fileStore = new Mock<IAIFileStore>();
            fileStore
                .Setup(x => x.ResolveAsync(ConversationId.ToString(), "file-abc", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIStoredFile { Data = SomeBytes, MimeType = "image/png", Filename = "a.png" });

            var result = await CreateProvider(fileStore).RehydrateAttachmentsAsync(ConversationId, message);

            var dataContent = result.Contents[0].ShouldBeOfType<DataContent>();
            dataContent.Data.ToArray().ShouldBe(SomeBytes);
            dataContent.Name.ShouldBe("a.png");
        }

        [Fact]
        public async Task RehydrateAttachmentsAsync_ReferenceNoLongerInFileStore_ReplacesWithPlaceholder()
        {
            var message = new ChatMessage(ChatRole.User, [Reference("file-gone")]);
            var fileStore = new Mock<IAIFileStore>();
            fileStore
                .Setup(x => x.ResolveAsync(ConversationId.ToString(), "file-gone", It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIStoredFile?)null);

            var result = await CreateProvider(fileStore).RehydrateAttachmentsAsync(ConversationId, message);

            var text = result.Contents[0].ShouldBeOfType<TextContent>();
            text.Text.ShouldBe(ConversationChatHistoryProvider.MissingAttachmentPlaceholder);
        }

        [Fact]
        public async Task RehydrateAttachmentsAsync_MessageWithNoReferences_IsUnaffected()
        {
            var message = new ChatMessage(ChatRole.User, "just text");
            var fileStore = new Mock<IAIFileStore>(MockBehavior.Strict);

            var result = await CreateProvider(fileStore).RehydrateAttachmentsAsync(ConversationId, message);

            result.ShouldBeSameAs(message);
        }

        /// <summary>
        /// A real reply loads its reference from <see cref="AIMessage.ContentJson"/> — a genuine
        /// serialize-then-deserialize round trip, not a reference built directly in memory like the
        /// other tests above. System.Text.Json deserializes an untyped dictionary value as
        /// <see cref="System.Text.Json.JsonElement"/> rather than the original <see cref="string"/>, so
        /// a rehydration check that only matched <c>string</c> would silently skip every reply — exactly
        /// the bug this test exists to catch (it passed against the in-memory-built reference above
        /// while failing for real, on a second turn, in the running app).
        /// </summary>
        [Fact]
        public async Task RehydrateAttachmentsAsync_ReferenceLoadedFromJson_StillResolves()
        {
            var storedMessage = new ChatMessage(ChatRole.User, [Reference("file-json")]);
            var contentJson = System.Text.Json.JsonSerializer.Serialize(storedMessage, Microsoft.Extensions.AI.AIJsonUtilities.DefaultOptions);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<ChatMessage>(contentJson, Microsoft.Extensions.AI.AIJsonUtilities.DefaultOptions)!;

            // Confirms the round trip actually reproduces the JsonElement case rather than testing nothing.
            var marker = ((UriContent)deserialized.Contents[0]).AdditionalProperties![AIFileContentMarker.FileIdPropertyKey];
            marker.ShouldBeOfType<System.Text.Json.JsonElement>();

            var fileStore = new Mock<IAIFileStore>();
            fileStore
                .Setup(x => x.ResolveAsync(ConversationId.ToString(), "file-json", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIStoredFile { Data = SomeBytes, MimeType = "image/png", Filename = "a.png" });

            var result = await CreateProvider(fileStore).RehydrateAttachmentsAsync(ConversationId, deserialized);

            var dataContent = result.Contents[0].ShouldBeOfType<DataContent>();
            dataContent.Data.ToArray().ShouldBe(SomeBytes);
        }
    }
}

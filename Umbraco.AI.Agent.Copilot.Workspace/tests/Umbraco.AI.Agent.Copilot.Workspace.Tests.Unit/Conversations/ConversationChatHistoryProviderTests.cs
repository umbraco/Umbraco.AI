using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
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
        => new(
            Mock.Of<IAIConversationRepository>(),
            (fileStore ?? new Mock<IAIFileStore>()).Object,
            NullLogger<ConversationChatHistoryProvider>.Instance);

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

    /// <summary>
    /// A browser refresh/close before Approve/Deny leaves a <see cref="ToolApprovalRequestContent"/> in
    /// persisted history with no matching <see cref="ToolApprovalResponseContent"/> — MAF's
    /// <c>ChatHistoryProvider</c> would otherwise concatenate it into every future turn and
    /// <c>FunctionInvokingChatClient</c> would throw. <see cref="ConversationChatHistoryProvider.GetDanglingApprovalRequestsAsync"/>
    /// is how the caller finds these so it can synthesize a deny before that happens.
    /// </summary>
    public class DanglingApprovalRequests
    {
        private static AIMessage ToStoredMessage(ChatMessage message)
            => new() { ContentJson = JsonSerializer.Serialize(message, AIJsonUtilities.DefaultOptions) };

        private static Mock<IAIConversationRepository> RepositoryReturning(params ChatMessage[] messages)
        {
            var repository = new Mock<IAIConversationRepository>();
            repository
                .Setup(x => x.GetMessagesAsync(ConversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages.Select(ToStoredMessage).ToList());
            return repository;
        }

        private static ConversationChatHistoryProvider CreateProvider(Mock<IAIConversationRepository> repository)
            => new(repository.Object, Mock.Of<IAIFileStore>(), NullLogger<ConversationChatHistoryProvider>.Instance);

        [Fact]
        public async Task GetDanglingApprovalRequestsAsync_RequestWithNoResponse_IsReturned()
        {
            var toolCall = new FunctionCallContent("call-1", "delete_umbraco_content");
            var request = new ToolApprovalRequestContent("req-1", toolCall);
            var provider = CreateProvider(RepositoryReturning(new ChatMessage(ChatRole.Assistant, [request])));

            var dangling = await provider.GetDanglingApprovalRequestsAsync(ConversationId);

            dangling.Single().ToolCall.CallId.ShouldBe("call-1");
        }

        [Fact]
        public async Task GetDanglingApprovalRequestsAsync_RequestWithMatchingResponse_IsNotReturned()
        {
            var toolCall = new FunctionCallContent("call-1", "delete_umbraco_content");
            var request = new ToolApprovalRequestContent("req-1", toolCall);
            var provider = CreateProvider(RepositoryReturning(
                new ChatMessage(ChatRole.Assistant, [request]),
                new ChatMessage(ChatRole.User, [request.CreateResponse(true)])));

            var dangling = await provider.GetDanglingApprovalRequestsAsync(ConversationId);

            dangling.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDanglingApprovalRequestsAsync_OneOfTwoRequestsResolved_ReturnsOnlyTheUnresolvedOne()
        {
            var resolvedCall = new FunctionCallContent("call-resolved", "publish_umbraco_content");
            var resolvedRequest = new ToolApprovalRequestContent("req-resolved", resolvedCall);
            var danglingCall = new FunctionCallContent("call-dangling", "delete_umbraco_content");
            var danglingRequest = new ToolApprovalRequestContent("req-dangling", danglingCall);
            var provider = CreateProvider(RepositoryReturning(
                new ChatMessage(ChatRole.Assistant, [resolvedRequest]),
                new ChatMessage(ChatRole.User, [resolvedRequest.CreateResponse(false)]),
                new ChatMessage(ChatRole.Assistant, [danglingRequest])));

            var dangling = await provider.GetDanglingApprovalRequestsAsync(ConversationId);

            dangling.Single().ToolCall.CallId.ShouldBe("call-dangling");
        }

        [Fact]
        public async Task GetDanglingApprovalRequestsAsync_NoApprovalContentAtAll_ReturnsEmpty()
        {
            var provider = CreateProvider(RepositoryReturning(new ChatMessage(ChatRole.User, "Hello")));

            var dangling = await provider.GetDanglingApprovalRequestsAsync(ConversationId);

            dangling.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDanglingApprovalRequestsAsync_EmptyConversationId_ReturnsEmptyWithoutCallingRepository()
        {
            var repository = new Mock<IAIConversationRepository>(MockBehavior.Strict);
            var provider = CreateProvider(repository);

            var dangling = await provider.GetDanglingApprovalRequestsAsync(Guid.Empty);

            dangling.ShouldBeEmpty();
        }
    }

    /// <summary>
    /// Defence in depth against the client-side conversation-message-duplication bug: a client whose
    /// not-yet-persisted boundary is corrupted re-sends messages the server already holds as "new".
    /// <see cref="ConversationChatHistoryProvider.DropAlreadyPersistedTailAsync"/> is the last line of
    /// defence in <c>StoreChatHistoryAsync</c> that catches this before it double-writes history.
    /// </summary>
    public class DropAlreadyPersistedTail
    {
        private static AIMessage Message(string role, string content) => new() { Role = role, ContentJson = content };

        /// <summary>Builds an <see cref="AIMessage"/> with real, production-shaped <c>ContentJson</c> — a
        /// serialized <see cref="ChatMessage"/>, optionally carrying a <see cref="ChatMessage.MessageId"/> —
        /// so tests can exercise the id-comparison path in <c>IsSameContent</c> rather than only its
        /// content-comparison fallback (which is all the plain-text <see cref="Message"/> helper above can
        /// reach, since its non-JSON <c>ContentJson</c> always fails to deserialize).</summary>
        private static AIMessage SerializedMessage(string role, string content, string? messageId = null)
        {
            var chatMessage = new ChatMessage(new ChatRole(role), content) { MessageId = messageId };
            return new AIMessage
            {
                Role = role,
                ContentJson = JsonSerializer.Serialize(chatMessage, AIJsonUtilities.DefaultOptions),
            };
        }

        private static Mock<IAIConversationRepository> RepositoryReturning(params AIMessage[] stored)
        {
            var repository = new Mock<IAIConversationRepository>();
            repository
                .Setup(x => x.GetMessagesAsync(ConversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stored.ToList());
            return repository;
        }

        private static ConversationChatHistoryProvider CreateProvider(Mock<IAIConversationRepository> repository)
            => new(repository.Object, Mock.Of<IAIFileStore>(), NullLogger<ConversationChatHistoryProvider>.Instance);

        [Fact]
        public async Task NewConversation_NoStoredMessages_ReturnsCandidatesUnchanged()
        {
            var provider = CreateProvider(RepositoryReturning());
            AIMessage[] candidates = [Message("user", "Hello")];

            var result = await provider.DropAlreadyPersistedTailAsync(ConversationId, candidates);

            result.ShouldBe(candidates);
        }

        [Fact]
        public async Task CandidatesDoNotOverlapStoredTail_ReturnsCandidatesUnchanged()
        {
            var provider = CreateProvider(RepositoryReturning(Message("assistant", "Hi")));
            AIMessage[] candidates = [Message("user", "A genuinely new message")];

            var result = await provider.DropAlreadyPersistedTailAsync(ConversationId, candidates);

            result.ShouldBe(candidates);
        }

        [Fact]
        public async Task LeadingRunDuplicatesStoredTail_IsDroppedKeepingOnlyTheGenuinelyNewMessage()
        {
            // The client re-sent the previous turn (already persisted) plus one genuinely new message —
            // exactly what a corrupted `#persisted` boundary produces.
            var provider = CreateProvider(RepositoryReturning(
                Message("user", "Hello"),
                Message("assistant", "Hi there")));
            AIMessage[] candidates =
            [
                Message("user", "Hello"),
                Message("assistant", "Hi there"),
                Message("user", "How are you?"),
            ];

            var result = await provider.DropAlreadyPersistedTailAsync(ConversationId, candidates);

            result.Count.ShouldBe(1);
            result[0].Role.ShouldBe("user");
            result[0].ContentJson.ShouldBe("How are you?");
        }

        [Fact]
        public async Task EntireCandidateListDuplicatesStoredTail_ReturnsEmpty()
        {
            var provider = CreateProvider(RepositoryReturning(
                Message("user", "Hello"),
                Message("assistant", "Hi there")));
            AIMessage[] candidates =
            [
                Message("user", "Hello"),
                Message("assistant", "Hi there"),
            ];

            var result = await provider.DropAlreadyPersistedTailAsync(ConversationId, candidates);

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task SameRoleButDifferentContent_IsNotTreatedAsADuplicate()
        {
            var provider = CreateProvider(RepositoryReturning(Message("user", "Hello")));
            AIMessage[] candidates = [Message("user", "Hello again, but different")];

            var result = await provider.DropAlreadyPersistedTailAsync(ConversationId, candidates);

            result.ShouldBe(candidates);
        }

        [Fact]
        public async Task SameMessageId_IsTreatedAsADuplicate()
        {
            // A genuine client re-send of the same message carries the same client-issued id.
            var provider = CreateProvider(RepositoryReturning(SerializedMessage("user", "Hello", "msg-1")));
            AIMessage[] candidates = [SerializedMessage("user", "Hello", "msg-1")];

            var result = await provider.DropAlreadyPersistedTailAsync(ConversationId, candidates);

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task SameContentDifferentMessageId_IsNotTreatedAsADuplicate()
        {
            // Two genuinely different messages that happen to share identical text (the user replying
            // "ok" twice in separate turns, say) must never be mistaken for a re-send — this is exactly
            // the false positive pure content-comparison risked before ids were compared.
            var provider = CreateProvider(RepositoryReturning(SerializedMessage("user", "ok", "msg-1")));
            AIMessage[] candidates = [SerializedMessage("user", "ok", "msg-2")];

            var result = await provider.DropAlreadyPersistedTailAsync(ConversationId, candidates);

            result.ShouldBe(candidates);
        }

        [Fact]
        public async Task NoMessageIdOnEitherSide_FallsBackToContentComparison()
        {
            // A response message the model itself never assigned an id to (or a row stored before ids
            // were carried through) still gets deduped, via the same role + identical content it always
            // used to be compared on.
            var provider = CreateProvider(RepositoryReturning(SerializedMessage("assistant", "Hi there")));
            AIMessage[] candidates = [SerializedMessage("assistant", "Hi there")];

            var result = await provider.DropAlreadyPersistedTailAsync(ConversationId, candidates);

            result.ShouldBeEmpty();
        }
    }
}

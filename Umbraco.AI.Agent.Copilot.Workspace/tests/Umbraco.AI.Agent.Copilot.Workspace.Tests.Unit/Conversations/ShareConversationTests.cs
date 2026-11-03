using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Tests for <see cref="AIConversationShareService.ShareConversationAsync"/>: creating or updating a
/// per-recipient share on one of the acting user's own conversations, resolving the Snapshot cutoff
/// server-side, and publishing <see cref="AIConversationShareCreatedNotification"/> only for a
/// genuinely new recipient. The frontend picker only ever submits one recipient per call, but the
/// service method itself still accepts a collection — the multi-recipient test below guards that
/// capability even though no current UI path exercises it.
/// </summary>
public class ShareConversationTests
{
    private static readonly Guid OwnerKey = Guid.NewGuid();
    private static readonly Guid RecipientKey = Guid.NewGuid();
    private static readonly Guid RecipientKey2 = Guid.NewGuid();

    #region Happy path

    [Fact]
    public async Task ShareConversationAsync_SnapshotScope_SetsCutoffToCurrentLastMessage()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var lastMessageId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        SetupOwnedConversation(conversations, conversationId);
        SetupLastMessage(conversations, conversationId, lastMessageId);
        shares
            .Setup(r => r.SaveAsync(It.IsAny<AIConversationShare>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConversationShare share, CancellationToken _) => (share, true));

        // Act
        await service.ShareConversationAsync(conversationId, [RecipientKey], AIConversationShareScopeMode.Snapshot);

        // Assert
        shares.Verify(
            r => r.SaveAsync(
                It.Is<AIConversationShare>(s => s.CutoffMessageId == lastMessageId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ShareConversationAsync_MultipleRecipientKeysInOneCall_CreatesOneShareRowPerRecipient()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        SetupOwnedConversation(conversations, conversationId);
        shares
            .Setup(r => r.SaveAsync(It.IsAny<AIConversationShare>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConversationShare share, CancellationToken _) => (share, true));

        // Act
        await service.ShareConversationAsync(
            conversationId, [RecipientKey, RecipientKey2], AIConversationShareScopeMode.Live);

        // Assert
        shares.Verify(
            r => r.SaveAsync(
                It.Is<AIConversationShare>(s => s.SharedWithUserKey == RecipientKey),
                It.IsAny<CancellationToken>()),
            Times.Once);
        shares.Verify(
            r => r.SaveAsync(
                It.Is<AIConversationShare>(s => s.SharedWithUserKey == RecipientKey2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ShareConversationAsync_LiveScope_LeavesCutoffMessageIdNull()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        SetupOwnedConversation(conversations, conversationId);
        shares
            .Setup(r => r.SaveAsync(It.IsAny<AIConversationShare>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConversationShare share, CancellationToken _) => (share, true));

        // Act
        await service.ShareConversationAsync(conversationId, [RecipientKey], AIConversationShareScopeMode.Live);

        // Assert — Live never needs a cutoff, so the message-paging lookup is never even made.
        shares.Verify(
            r => r.SaveAsync(It.Is<AIConversationShare>(s => s.CutoffMessageId == null), It.IsAny<CancellationToken>()),
            Times.Once);
        conversations.Verify(
            c => c.GetMessagesPagedAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ShareConversationAsync_RecipientAlreadyShared_UpdatesExistingRowInstead()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, aggregator) = BuildService();
        SetupOwnedConversation(conversations, conversationId);
        shares
            .Setup(r => r.SaveAsync(It.IsAny<AIConversationShare>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConversationShare share, CancellationToken _) => (share, false));

        // Act
        await service.ShareConversationAsync(conversationId, [RecipientKey], AIConversationShareScopeMode.Live);

        // Assert — an update to an already-active recipient never sends the new-share notification.
        aggregator.Verify(
            a => a.PublishAsync(It.IsAny<AIConversationShareCreatedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ShareConversationAsync_NewShare_PublishesCreatedNotificationWithEventMessages()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, aggregator) = BuildService();
        var conversation = SetupOwnedConversation(conversations, conversationId);
        shares
            .Setup(r => r.SaveAsync(It.IsAny<AIConversationShare>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConversationShare share, CancellationToken _) => (share, true));

        AIConversationShareCreatedNotification? published = null;
        aggregator
            .Setup(a => a.PublishAsync(It.IsAny<AIConversationShareCreatedNotification>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((n, _) => published = (AIConversationShareCreatedNotification)n)
            .Returns(Task.CompletedTask);

        // Act
        await service.ShareConversationAsync(conversationId, [RecipientKey], AIConversationShareScopeMode.Live);

        // Assert
        published.ShouldNotBeNull();
        published.Conversation.ShouldBe(conversation);
        published.Share.SharedWithUserKey.ShouldBe(RecipientKey);
        published.Messages.ShouldNotBeNull();
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task ShareConversationAsync_NotOwned_ThrowsAndCreatesNoShare()
    {
        // Arrange — another user's conversation is indistinguishable from a missing one.
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        conversations
            .Setup(c => c.GetConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConversation?)null);

        // Act / Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.ShareConversationAsync(conversationId, [RecipientKey], AIConversationShareScopeMode.Live));
        shares.Verify(
            r => r.SaveAsync(It.IsAny<AIConversationShare>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ShareConversationAsync_SnapshotWithNoMessages_ThrowsRatherThanFallingBackToLive()
    {
        // Arrange — zero messages: nothing exists to anchor a Snapshot cutoff to.
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        SetupOwnedConversation(conversations, conversationId);
        conversations
            .Setup(c => c.GetMessagesPagedAsync(conversationId, 0, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<AIMessage>(), 0));

        // Act / Assert
        var exception = await Should.ThrowAsync<AIConversationShareCutoffUnresolvableException>(
            () => service.ShareConversationAsync(conversationId, [RecipientKey], AIConversationShareScopeMode.Snapshot));
        exception.ConversationId.ShouldBe(conversationId);
        shares.Verify(
            r => r.SaveAsync(It.IsAny<AIConversationShare>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ShareConversationAsync_SnapshotRaceEmptiesTailBetweenCountAndFetch_ThrowsRatherThanFallingBackToLive()
    {
        // Arrange — the count call sees one message, but a concurrent regenerate removes it before the
        // follow-up fetch for the last page runs.
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        SetupOwnedConversation(conversations, conversationId);
        conversations
            .Setup(c => c.GetMessagesPagedAsync(conversationId, 0, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<AIMessage>(), 1));
        conversations
            .Setup(c => c.GetMessagesPagedAsync(conversationId, 0, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<AIMessage>(), 1));

        // Act / Assert
        var exception = await Should.ThrowAsync<AIConversationShareCutoffUnresolvableException>(
            () => service.ShareConversationAsync(conversationId, [RecipientKey], AIConversationShareScopeMode.Snapshot));
        exception.ConversationId.ShouldBe(conversationId);
        shares.Verify(
            r => r.SaveAsync(It.IsAny<AIConversationShare>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ShareConversationAsync_RecipientIsOwner_ThrowsAndCreatesNoShare()
    {
        // Arrange — sharing with yourself would show up oddly in your own "Shared with me" list.
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        SetupOwnedConversation(conversations, conversationId);

        // Act / Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.ShareConversationAsync(conversationId, [OwnerKey], AIConversationShareScopeMode.Live));
        shares.Verify(
            r => r.SaveAsync(It.IsAny<AIConversationShare>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    /// <summary>Not yet exercised: resolving the service through the real Composer-registered DI graph.</summary>
    [Fact(Skip = "Pending real DI resolution — verified live in SC-10")]
    public Task ShareConversationAsync_ResolvedFromDI_SharesSuccessfully() => Task.CompletedTask;

    private static AIConversation SetupOwnedConversation(Mock<IAIConversationService> conversations, Guid conversationId)
    {
        var conversation = new AIConversation { Id = conversationId, UserKey = OwnerKey };
        conversations
            .Setup(c => c.GetConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        return conversation;
    }

    private static void SetupLastMessage(Mock<IAIConversationService> conversations, Guid conversationId, Guid lastMessageId)
    {
        conversations
            .Setup(c => c.GetMessagesPagedAsync(conversationId, 0, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<AIMessage>(), 1));
        conversations
            .Setup(c => c.GetMessagesPagedAsync(conversationId, 0, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { new AIMessage { Id = lastMessageId } }, 1));
    }

    private static (
        AIConversationShareService Service,
        Mock<IAIConversationService> Conversations,
        Mock<IAIConversationShareRepository> Shares,
        Mock<IEventAggregator> Aggregator) BuildService()
    {
        var conversations = new Mock<IAIConversationService>();
        var shares = new Mock<IAIConversationShareRepository>();
        var aggregator = new Mock<IEventAggregator>();

        var user = new Mock<IUser>();
        user.Setup(x => x.Key).Returns(OwnerKey);
        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(x => x.CurrentUser).Returns(user.Object);
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(x => x.BackOfficeSecurity).Returns(security.Object);

        var service = new AIConversationShareService(shares.Object, conversations.Object, accessor.Object, aggregator.Object);
        return (service, conversations, shares, aggregator);
    }
}

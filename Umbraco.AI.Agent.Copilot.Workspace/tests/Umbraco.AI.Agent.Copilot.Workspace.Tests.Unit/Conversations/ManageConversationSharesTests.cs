using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Tests for the owner-side listing and revoking of a conversation's shares —
/// <see cref="AIConversationShareService.GetConversationSharesAsync"/> and
/// <see cref="AIConversationShareService.RevokeConversationShareAsync"/>. Both enforce the same
/// ownership boundary as <see cref="AIConversationShareService.ShareConversationAsync"/>: not-found
/// and not-owned are deliberately indistinguishable.
/// </summary>
public class ManageConversationSharesTests
{
    private static readonly Guid OwnerKey = Guid.NewGuid();
    private static readonly Guid RecipientKey = Guid.NewGuid();

    #region Happy path

    [Fact]
    public async Task GetConversationSharesAsync_ConversationSharedWithTwo_ReturnsBothWithScopeAndDate()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        SetupOwnedConversation(conversations, conversationId);
        var expected = new[]
        {
            new AIConversationShare
            {
                ConversationId = conversationId,
                SharedWithUserKey = RecipientKey,
                ScopeMode = AIConversationShareScopeMode.Live,
                DateShared = DateTime.UtcNow,
            },
            new AIConversationShare
            {
                ConversationId = conversationId,
                SharedWithUserKey = Guid.NewGuid(),
                ScopeMode = AIConversationShareScopeMode.Snapshot,
                DateShared = DateTime.UtcNow,
            },
        };
        shares
            .Setup(r => r.GetActiveByConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await service.GetConversationSharesAsync(conversationId);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public async Task RevokeConversationShareAsync_ActiveShare_RevokesSuccessfully()
    {
        // Arrange — soft-revoke itself (setting DateRevoked) is the repository's job; the service's
        // contract is simply that an active share's revoke does not throw.
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        SetupOwnedConversation(conversations, conversationId);
        shares
            .Setup(r => r.RevokeAsync(conversationId, RecipientKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act / Assert
        await Should.NotThrowAsync(() => service.RevokeConversationShareAsync(conversationId, RecipientKey));
        shares.Verify(r => r.RevokeAsync(conversationId, RecipientKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task GetConversationSharesAsync_NotOwned_ThrowsAndReturnsNothing()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        conversations
            .Setup(c => c.GetConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConversation?)null);

        // Act / Assert
        await Should.ThrowAsync<InvalidOperationException>(() => service.GetConversationSharesAsync(conversationId));
        shares.Verify(
            r => r.GetActiveByConversationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeConversationShareAsync_NotOwned_ThrowsAndDoesNotRevoke()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        conversations
            .Setup(c => c.GetConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConversation?)null);

        // Act / Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.RevokeConversationShareAsync(conversationId, RecipientKey));
        shares.Verify(
            r => r.RevokeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeConversationShareAsync_AlreadyRevokedOrNeverExisted_ThrowsRatherThanNoOpSucceeding()
    {
        // Arrange — a second revoke (or one for a recipient who was never shared with) is a real
        // failure, not silently accepted.
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares, _) = BuildService();
        SetupOwnedConversation(conversations, conversationId);
        shares
            .Setup(r => r.RevokeAsync(conversationId, RecipientKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act / Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.RevokeConversationShareAsync(conversationId, RecipientKey));
    }

    #endregion

    /// <summary>
    /// Spans both the owner-side write service (<see cref="AIConversationShareService"/>, SC-04) and
    /// the recipient-side read service (<see cref="AIConversationShareReadService"/>, SC-07): revoking
    /// a share must close off the recipient's read path immediately. The two services don't share
    /// state directly — this guards their shared contract through the repository they both depend on
    /// (<see cref="IAIConversationShareRepository"/>): once <c>RevokeAsync</c> has run, the read
    /// service's active-share lookup must come back empty for that recipient.
    /// </summary>
    [Fact]
    public async Task RevokeConversationShareAsync_ThenGetSharedConversationAsync_NoLongerResolves()
    {
        // Arrange — the shared repository mock is genuinely stateful here, so the test exercises the
        // real interaction between SC-04's write service and SC-07's read service through the
        // repository they both depend on, rather than two independently-stubbed halves.
        var conversationId = Guid.NewGuid();
        var (writeService, conversations, shares, _) = BuildService();
        SetupOwnedConversation(conversations, conversationId);

        AIConversationShare? active = new AIConversationShare
        {
            ConversationId = conversationId,
            SharedWithUserKey = RecipientKey,
            SharedByUserKey = OwnerKey,
            ScopeMode = AIConversationShareScopeMode.Live,
            DateShared = DateTime.UtcNow,
        };
        shares
            .Setup(r => r.GetActiveShareAsync(conversationId, RecipientKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => active);
        shares
            .Setup(r => r.RevokeAsync(conversationId, RecipientKey, It.IsAny<CancellationToken>()))
            .Callback(() => active = null)
            .ReturnsAsync(true);

        var readRepository = new Mock<IAIConversationRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = OwnerKey });
        readRepository
            .Setup(r => r.GetMessagesAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var recipientUser = new Mock<IUser>();
        recipientUser.Setup(x => x.Key).Returns(RecipientKey);
        var recipientSecurity = new Mock<IBackOfficeSecurity>();
        recipientSecurity.Setup(x => x.CurrentUser).Returns(recipientUser.Object);
        var recipientAccessor = new Mock<IBackOfficeSecurityAccessor>();
        recipientAccessor.Setup(x => x.BackOfficeSecurity).Returns(recipientSecurity.Object);
        var readService = new AIConversationShareReadService(shares.Object, readRepository.Object, recipientAccessor.Object);

        // Act / Assert — resolves while the share is active, then stops resolving once revoked.
        var beforeRevoke = await readService.GetSharedConversationAsync(conversationId);
        beforeRevoke.ShouldNotBeNull();

        await writeService.RevokeConversationShareAsync(conversationId, RecipientKey);

        var afterRevoke = await readService.GetSharedConversationAsync(conversationId);
        afterRevoke.ShouldBeNull();

        shares.Verify(r => r.RevokeAsync(conversationId, RecipientKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void SetupOwnedConversation(Mock<IAIConversationService> conversations, Guid conversationId)
    {
        conversations
            .Setup(c => c.GetConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = OwnerKey });
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

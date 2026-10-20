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
    /// Not yet exercised: the recipient-facing read path this revoke must close off immediately, which
    /// lives on a service that doesn't exist yet.
    /// </summary>
    [Fact(Skip = "Pending the recipient-side read service — SC-07")]
    public Task RevokeConversationShareAsync_ThenGetSharedConversationAsync_NoLongerResolves() => Task.CompletedTask;

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

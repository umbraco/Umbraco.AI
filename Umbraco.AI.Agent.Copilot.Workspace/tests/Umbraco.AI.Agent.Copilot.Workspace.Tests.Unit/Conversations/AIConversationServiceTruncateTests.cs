using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Tests for the regenerate truncation on <see cref="AIConversationService"/>: dropping everything after
/// a conversation's last user message so the next run replaces that answer instead of appending a second
/// one. Ownership is enforced the same way as every other write, and the cutoff itself is the
/// repository's job (the service never passes positions).
/// </summary>
public class AIConversationServiceTruncateTests
{
    private static readonly Guid UserKey = Guid.NewGuid();

    [Fact]
    public async Task TruncateAfterLastUserMessageAsync_WhenOwned_DeletesAndPublishesSaved()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, repository, aggregator) = BuildService();
        repository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = UserKey });
        repository
            .Setup(r => r.DeleteMessagesAfterLastUserMessageAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var deleted = await service.TruncateAfterLastUserMessageAsync(conversationId);

        // Assert
        deleted.ShouldBe(3);
        repository.Verify(r => r.DeleteMessagesAfterLastUserMessageAsync(conversationId, It.IsAny<CancellationToken>()), Times.Once);
        aggregator.Verify(x => x.PublishAsync(It.IsAny<AIConversationSavedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TruncateAfterLastUserMessageAsync_WhenNotOwned_ThrowsAndDoesNotDelete()
    {
        // Arrange — another user's conversation is indistinguishable from a missing one.
        var conversationId = Guid.NewGuid();
        var (service, repository, _) = BuildService();
        repository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = Guid.NewGuid() });

        // Act / Assert
        await Should.ThrowAsync<InvalidOperationException>(() => service.TruncateAfterLastUserMessageAsync(conversationId));
        repository.Verify(
            r => r.DeleteMessagesAfterLastUserMessageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TruncateAfterLastUserMessageAsync_WhenSavingCancelled_ThrowsAndDoesNotDelete()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, repository, aggregator) = BuildService();
        repository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = UserKey });
        aggregator
            .Setup(x => x.PublishAsync(It.IsAny<AIConversationSavingNotification>(), It.IsAny<CancellationToken>()))
            .Callback<AIConversationSavingNotification, CancellationToken>((n, _) => n.Cancel = true)
            .Returns(Task.CompletedTask);

        // Act / Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => service.TruncateAfterLastUserMessageAsync(conversationId));
        ex.Message.ShouldContain("cancelled");
        repository.Verify(
            r => r.DeleteMessagesAfterLastUserMessageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetLastUserMessageTextAsync_WhenOwned_ReturnsStoredText()
    {
        // Arrange — this is what prompts agent auto-selection on a regenerate, where the run carries no
        // inbound user message of its own.
        var conversationId = Guid.NewGuid();
        var (service, repository, _) = BuildService();
        repository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = UserKey });
        repository
            .Setup(r => r.GetLastUserMessageTextAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("summarise this page");

        // Act
        var text = await service.GetLastUserMessageTextAsync(conversationId);

        // Assert
        text.ShouldBe("summarise this page");
    }

    [Fact]
    public async Task GetLastUserMessageTextAsync_WhenNotOwned_Throws()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, repository, _) = BuildService();
        repository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = Guid.NewGuid() });

        // Act / Assert
        await Should.ThrowAsync<InvalidOperationException>(() => service.GetLastUserMessageTextAsync(conversationId));
        repository.Verify(
            r => r.GetLastUserMessageTextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static (AIConversationService Service, Mock<IAIConversationRepository> Repository, Mock<IEventAggregator> Aggregator) BuildService()
    {
        var repository = new Mock<IAIConversationRepository>();
        var fileStore = new Mock<IAIFileStore>();
        var aggregator = new Mock<IEventAggregator>();

        var user = new Mock<IUser>();
        user.Setup(x => x.Key).Returns(UserKey);
        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(x => x.CurrentUser).Returns(user.Object);
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(x => x.BackOfficeSecurity).Returns(security.Object);

        var service = new AIConversationService(repository.Object, fileStore.Object, accessor.Object, aggregator.Object);
        return (service, repository, aggregator);
    }
}

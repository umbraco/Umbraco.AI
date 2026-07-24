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
/// Tests for the conversation lifecycle notifications published by <see cref="AIConversationService"/>
/// (Saving/Saved on create and update, Deleting/Deleted on delete). Archiving is a field toggle on
/// update and so flows through the same Saving/Saved path.
/// </summary>
public class AIConversationServiceNotificationTests
{
    private static readonly Guid UserKey = Guid.NewGuid();

    [Fact]
    public async Task CreateConversationAsync_PublishesSavedAfterCreate()
    {
        // Arrange
        var (service, repository, aggregator, _) = BuildService();
        var conversation = new AIConversation { Id = Guid.NewGuid() };
        repository
            .Setup(r => r.CreateAsync(It.IsAny<AIConversation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        await service.CreateConversationAsync(conversation);

        // Assert
        aggregator.Verify(x => x.PublishAsync(It.IsAny<AIConversationSavedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateConversationAsync_WhenSavingCancelled_ThrowsAndDoesNotCreate()
    {
        // Arrange
        var (service, repository, aggregator, _) = BuildService();
        aggregator
            .Setup(x => x.PublishAsync(It.IsAny<AIConversationSavingNotification>(), It.IsAny<CancellationToken>()))
            .Callback<AIConversationSavingNotification, CancellationToken>((n, _) => n.Cancel = true)
            .Returns(Task.CompletedTask);

        // Act / Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateConversationAsync(new AIConversation { Id = Guid.NewGuid() }));
        ex.Message.ShouldContain("cancelled");
        repository.Verify(r => r.CreateAsync(It.IsAny<AIConversation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConversationAsync_WhenDeletingCancelled_ThrowsAndDoesNotDelete()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var (service, repository, aggregator, fileStore) = BuildService();
        repository
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = UserKey });
        aggregator
            .Setup(x => x.PublishAsync(It.IsAny<AIConversationDeletingNotification>(), It.IsAny<CancellationToken>()))
            .Callback<AIConversationDeletingNotification, CancellationToken>((n, _) => n.Cancel = true)
            .Returns(Task.CompletedTask);

        // Act / Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => service.DeleteConversationAsync(conversationId));
        ex.Message.ShouldContain("cancelled");
        repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        fileStore.Verify(f => f.CleanupThreadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (AIConversationService Service, Mock<IAIConversationRepository> Repository, Mock<IEventAggregator> Aggregator, Mock<IAIFileStore> FileStore) BuildService()
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
        return (service, repository, aggregator, fileStore);
    }
}

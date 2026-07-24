using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.Cms.Core.Events;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Projects;

/// <summary>
/// Tests for <see cref="AIProjectDeletingNotificationHandler"/> — the guard that blocks deleting a
/// project while it still owns conversations.
/// </summary>
public class AIProjectDeletingNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenProjectHasConversations_CancelsWithInUseMessage()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var conversationService = new Mock<IAIConversationService>();
        conversationService
            .Setup(x => x.ConversationsExistInProjectAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new AIProjectDeletingNotificationHandler(conversationService.Object);
        var notification = new AIProjectDeletingNotification(projectId, new EventMessages());

        // Act
        await handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeTrue();
        notification.Messages.GetAll().ShouldContain(m => m.Message.Contains("in use"));
    }

    [Fact]
    public async Task HandleAsync_WhenProjectHasNoConversations_DoesNotCancel()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var conversationService = new Mock<IAIConversationService>();
        conversationService
            .Setup(x => x.ConversationsExistInProjectAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new AIProjectDeletingNotificationHandler(conversationService.Object);
        var notification = new AIProjectDeletingNotification(projectId, new EventMessages());

        // Act
        await handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeFalse();
        notification.Messages.GetAll().ShouldBeEmpty();
    }
}

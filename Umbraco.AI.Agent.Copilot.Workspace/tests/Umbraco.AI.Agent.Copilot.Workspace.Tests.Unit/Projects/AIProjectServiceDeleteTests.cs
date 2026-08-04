using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Projects;

/// <summary>
/// Tests for <see cref="AIProjectService"/> deletion — the delete publishes a cancelable
/// <see cref="AIProjectDeletingNotification"/> and only removes the project if no handler cancels it.
/// </summary>
public class AIProjectServiceDeleteTests
{
    private static readonly Guid UserKey = Guid.NewGuid();

    [Fact]
    public async Task DeleteProjectAsync_WhenNotificationCancelled_ThrowsInUseAndDoesNotDelete()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var repository = BuildRepositoryForOwnedProject(projectId);
        var aggregator = new Mock<IEventAggregator>();
        aggregator
            .Setup(x => x.PublishAsync(It.IsAny<AIProjectDeletingNotification>(), It.IsAny<CancellationToken>()))
            .Callback<AIProjectDeletingNotification, CancellationToken>((n, _) =>
            {
                n.Messages.Add(new EventMessage("Project in use", "Project is in use by one or more conversations.", EventMessageType.Error));
                n.Cancel = true;
            })
            .Returns(Task.CompletedTask);
        var service = new AIProjectService(repository.Object, BuildSecurityAccessor(), aggregator.Object);

        // Act / Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => service.DeleteProjectAsync(projectId));
        ex.Message.ShouldContain("in use");
        repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveProjectAsync_WhenSavingCancelled_ThrowsAndDoesNotSave()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var repository = BuildRepositoryForOwnedProject(projectId);
        var aggregator = new Mock<IEventAggregator>();
        aggregator
            .Setup(x => x.PublishAsync(It.IsAny<AIProjectSavingNotification>(), It.IsAny<CancellationToken>()))
            .Callback<AIProjectSavingNotification, CancellationToken>((n, _) =>
            {
                n.Messages.Add(new EventMessage("Blocked", "Nope.", EventMessageType.Error));
                n.Cancel = true;
            })
            .Returns(Task.CompletedTask);
        var service = new AIProjectService(repository.Object, BuildSecurityAccessor(), aggregator.Object);
        var project = new AIProject { Id = projectId, Name = "Docs", UserKey = UserKey };

        // Act / Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => service.SaveProjectAsync(project));
        ex.Message.ShouldContain("cancelled");
        repository.Verify(r => r.SaveAsync(It.IsAny<AIProject>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveProjectAsync_WhenNotCancelled_PublishesSavedAfterSave()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var repository = BuildRepositoryForOwnedProject(projectId);
        var project = new AIProject { Id = projectId, Name = "Docs", UserKey = UserKey };
        repository
            .Setup(r => r.SaveAsync(It.IsAny<AIProject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        var aggregator = new Mock<IEventAggregator>();
        aggregator
            .Setup(x => x.PublishAsync(It.IsAny<AIProjectSavingNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new AIProjectService(repository.Object, BuildSecurityAccessor(), aggregator.Object);

        // Act
        await service.SaveProjectAsync(project);

        // Assert
        aggregator.Verify(x => x.PublishAsync(It.IsAny<AIProjectSavedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProjectAsync_WhenNotCancelled_DeletesProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var repository = BuildRepositoryForOwnedProject(projectId);
        var aggregator = new Mock<IEventAggregator>();
        aggregator
            .Setup(x => x.PublishAsync(It.IsAny<AIProjectDeletingNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new AIProjectService(repository.Object, BuildSecurityAccessor(), aggregator.Object);

        // Act
        await service.DeleteProjectAsync(projectId);

        // Assert
        repository.Verify(r => r.DeleteAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IAIProjectRepository> BuildRepositoryForOwnedProject(Guid projectId)
    {
        var repository = new Mock<IAIProjectRepository>();
        repository
            .Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIProject { Id = projectId, Name = "Docs", UserKey = UserKey });
        return repository;
    }

    private static IBackOfficeSecurityAccessor BuildSecurityAccessor()
    {
        var user = new Mock<IUser>();
        user.Setup(x => x.Key).Returns(UserKey);
        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(x => x.CurrentUser).Returns(user.Object);
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(x => x.BackOfficeSecurity).Returns(security.Object);
        return accessor.Object;
    }
}

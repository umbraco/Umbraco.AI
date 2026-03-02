using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core.Events;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.NotificationHandlers;

public class AIProfileDeletingAgentNotificationHandlerTests
{
    private readonly Mock<IAIAgentService> _agentServiceMock;
    private readonly AIProfileDeletingAgentNotificationHandler _handler;

    public AIProfileDeletingAgentNotificationHandlerTests()
    {
        _agentServiceMock = new Mock<IAIAgentService>();
        _handler = new AIProfileDeletingAgentNotificationHandler(_agentServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsInUseByAgents_CancelsNotification()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIProfileDeletingNotification(profileId, messages);

        _agentServiceMock
            .Setup(x => x.AgentsExistWithProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeTrue();
        messages.GetAll().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsNotInUseByAgents_DoesNotCancelNotification()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIProfileDeletingNotification(profileId, messages);

        _agentServiceMock
            .Setup(x => x.AgentsExistWithProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeFalse();
        messages.GetAll().ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsInUseByAgents_AddsErrorMessage()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIProfileDeletingNotification(profileId, messages);

        _agentServiceMock
            .Setup(x => x.AgentsExistWithProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        var allMessages = messages.GetAll().ToList();
        allMessages.Count.ShouldBe(1);
        allMessages[0].MessageType.ShouldBe(EventMessageType.Error);
        allMessages[0].Message.ShouldContain("agent");
    }
}

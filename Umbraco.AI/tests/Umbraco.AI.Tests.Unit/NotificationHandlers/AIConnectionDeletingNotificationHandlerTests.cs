using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Tests.Unit.NotificationHandlers;

public class AIConnectionDeletingNotificationHandlerTests
{
    private readonly Mock<IAIProfileService> _profileServiceMock;
    private readonly AIConnectionDeletingNotificationHandler _handler;

    public AIConnectionDeletingNotificationHandlerTests()
    {
        _profileServiceMock = new Mock<IAIProfileService>();
        _handler = new AIConnectionDeletingNotificationHandler(_profileServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenConnectionIsInUse_CancelsNotification()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIConnectionDeletingNotification(connectionId, messages);

        _profileServiceMock
            .Setup(x => x.ProfilesExistWithConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeTrue();
        messages.GetAll().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenConnectionIsNotInUse_DoesNotCancelNotification()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIConnectionDeletingNotification(connectionId, messages);

        _profileServiceMock
            .Setup(x => x.ProfilesExistWithConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeFalse();
        messages.GetAll().ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenConnectionIsInUse_AddsErrorMessage()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIConnectionDeletingNotification(connectionId, messages);

        _profileServiceMock
            .Setup(x => x.ProfilesExistWithConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        var allMessages = messages.GetAll().ToList();
        allMessages.Count.ShouldBe(1);
        allMessages[0].MessageType.ShouldBe(EventMessageType.Error);
        allMessages[0].Message.ShouldContain("in use");
    }
}

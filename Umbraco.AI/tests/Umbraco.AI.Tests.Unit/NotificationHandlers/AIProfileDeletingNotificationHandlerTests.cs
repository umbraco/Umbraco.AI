using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Tests.Unit.NotificationHandlers;

public class AIProfileDeletingNotificationHandlerTests
{
    private readonly Mock<IAISettingsService> _settingsServiceMock;
    private readonly AIProfileDeletingNotificationHandler _handler;

    public AIProfileDeletingNotificationHandlerTests()
    {
        _settingsServiceMock = new Mock<IAISettingsService>();
        _handler = new AIProfileDeletingNotificationHandler(_settingsServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsDefaultChatProfile_CancelsNotification()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIProfileDeletingNotification(profileId, messages);

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AISettings { DefaultChatProfileId = profileId });

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeTrue();
        messages.GetAll().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsDefaultEmbeddingProfile_CancelsNotification()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIProfileDeletingNotification(profileId, messages);

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AISettings { DefaultEmbeddingProfileId = profileId });

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeTrue();
        messages.GetAll().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsNotDefault_DoesNotCancelNotification()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIProfileDeletingNotification(profileId, messages);

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AISettings
            {
                DefaultChatProfileId = Guid.NewGuid(),
                DefaultEmbeddingProfileId = Guid.NewGuid()
            });

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeFalse();
        messages.GetAll().ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsDefault_AddsErrorMessage()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIProfileDeletingNotification(profileId, messages);

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AISettings { DefaultChatProfileId = profileId });

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        var allMessages = messages.GetAll().ToList();
        allMessages.Count.ShouldBe(1);
        allMessages[0].MessageType.ShouldBe(EventMessageType.Error);
        allMessages[0].Message.ShouldContain("default");
    }
}

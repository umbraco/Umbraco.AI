using Moq;
using Shouldly;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core.Events;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.NotificationHandlers;

public class AIProfileDeletingPromptNotificationHandlerTests
{
    private readonly Mock<IAIPromptService> _promptServiceMock;
    private readonly AIProfileDeletingPromptNotificationHandler _handler;

    public AIProfileDeletingPromptNotificationHandlerTests()
    {
        _promptServiceMock = new Mock<IAIPromptService>();
        _handler = new AIProfileDeletingPromptNotificationHandler(_promptServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsInUseByPrompts_CancelsNotification()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIProfileDeletingNotification(profileId, messages);

        _promptServiceMock
            .Setup(x => x.PromptsExistWithProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeTrue();
        messages.GetAll().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsNotInUseByPrompts_DoesNotCancelNotification()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIProfileDeletingNotification(profileId, messages);

        _promptServiceMock
            .Setup(x => x.PromptsExistWithProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        notification.Cancel.ShouldBeFalse();
        messages.GetAll().ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsInUseByPrompts_AddsErrorMessage()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var messages = new EventMessages();
        var notification = new AIProfileDeletingNotification(profileId, messages);

        _promptServiceMock
            .Setup(x => x.PromptsExistWithProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        var allMessages = messages.GetAll().ToList();
        allMessages.Count.ShouldBe(1);
        allMessages[0].MessageType.ShouldBe(EventMessageType.Error);
        allMessages[0].Message.ShouldContain("prompt");
    }
}

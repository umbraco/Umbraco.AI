using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Conversations;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Tests for <see cref="AIConversationShareCreatedNotificationHandler"/> — the handler that emails a
/// recipient when <see cref="AIConversationShareService.ShareConversationAsync"/> publishes
/// <see cref="AIConversationShareCreatedNotification"/> for a genuinely new share row. See
/// <see cref="ShareConversationTests"/> for the (already-covered) decision of when that notification
/// fires at all — this handler only reacts to it.
/// </summary>
public class AIConversationShareCreatedNotificationHandlerTests
{
    private static readonly Guid OwnerKey = Guid.NewGuid();
    private static readonly Guid RecipientKey = Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_NewShare_SendsEmailToRecipientWithOwnerNameAndResolvableLink()
    {
        // Arrange
        var (handler, userService, emailSender, _) = BuildHandler();
        SetUpUser(userService, RecipientKey, "Anders Andersen", "anders@example.com");
        SetUpUser(userService, OwnerKey, "Sofie Jensen", "sofie@example.com");
        var notification = BuildNotification(title: "Debugging the checkout redirect");
        var expectedUrl = $"https://my-site.example/umbraco/section/copilot-workspace/conversation/{Uri.EscapeDataString(notification.Conversation.Id.ToString())}";

        // Act
        await handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        emailSender.Verify(
            x => x.SendAsync(
                It.Is<EmailMessage>(m =>
                    m.To.Length == 1 &&
                    m.To[0] == "anders@example.com" &&
                    m.Subject!.Contains("Sofie Jensen") &&
                    m.Body!.Contains("Sofie Jensen") &&
                    m.Body!.Contains("Debugging the checkout redirect") &&
                    m.Body!.Contains(expectedUrl)),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TitleAndOwnerNameContainHtml_EncodesBeforeInterpolating()
    {
        // Arrange — untrusted values (title, names) must be HTML-encoded, never interpolated raw.
        var (handler, userService, emailSender, _) = BuildHandler();
        SetUpUser(userService, RecipientKey, "Anders Andersen", "anders@example.com");
        SetUpUser(userService, OwnerKey, "<script>alert(1)</script> \"Sofie\"", "sofie@example.com");
        var notification = BuildNotification(title: "<script>alert(2)</script> \"redirect\"");

        // Act
        await handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        emailSender.Verify(
            x => x.SendAsync(
                It.Is<EmailMessage>(m =>
                    m.Body!.Contains("&lt;script&gt;") &&
                    m.Body!.Contains("&quot;") &&
                    !m.Body!.Contains("<script>alert(1)</script>") &&
                    !m.Body!.Contains("<script>alert(2)</script>")),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RecipientHasNoEmail_LogsWarningAndDoesNotSend()
    {
        // Arrange
        var (handler, userService, emailSender, logger) = BuildHandler();
        SetUpUser(userService, RecipientKey, "Anders Andersen", email: null);
        var notification = BuildNotification();

        // Act
        await handler.HandleAsync(notification, CancellationToken.None);

        // Assert — a missing/incomplete user record is a skip, not a crash.
        emailSender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<TimeSpan?>()),
            Times.Never);
        VerifyLogged(logger, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_RecipientNotFound_LogsWarningAndDoesNotSend()
    {
        // Arrange
        var (handler, userService, emailSender, logger) = BuildHandler();
        userService
            .Setup(x => x.GetAsync(RecipientKey))
            .ReturnsAsync((IUser?)null);
        var notification = BuildNotification();

        // Act
        await handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        emailSender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<TimeSpan?>()),
            Times.Never);
        VerifyLogged(logger, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_OwnerNotResolvable_FallsBackToGenericOwnerNameRatherThanThrowing()
    {
        // Arrange — shouldn't normally happen (the owner is the authenticated user who just shared),
        // but a missing owner record must never throw out of this handler.
        var (handler, userService, emailSender, _) = BuildHandler();
        SetUpUser(userService, RecipientKey, "Anders Andersen", "anders@example.com");
        userService
            .Setup(x => x.GetAsync(OwnerKey))
            .ReturnsAsync((IUser?)null);
        var notification = BuildNotification();

        // Act
        await handler.HandleAsync(notification, CancellationToken.None);

        // Assert
        emailSender.Verify(
            x => x.SendAsync(
                It.Is<EmailMessage>(m => m.Subject!.Contains("A colleague")),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmailSenderThrows_LogsAndDoesNotRethrow()
    {
        // Arrange — a flaky SMTP server must never surface as an unhandled exception out of a
        // fire-and-forget handler reacting to an already-committed share.
        var (handler, userService, emailSender, logger) = BuildHandler();
        SetUpUser(userService, RecipientKey, "Anders Andersen", "anders@example.com");
        SetUpUser(userService, OwnerKey, "Sofie Jensen", "sofie@example.com");
        emailSender
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new InvalidOperationException("SMTP is down"));
        var notification = BuildNotification();

        // Act / Assert — must complete without throwing.
        await handler.HandleAsync(notification, CancellationToken.None);
        VerifyLogged(logger, LogLevel.Error);
    }

    [Fact]
    public async Task HandleAsync_UserServiceGetAsyncThrows_LogsAndDoesNotRethrow()
    {
        // Arrange — the whole handler body is wrapped, not just the SendAsync call, because an unhandled
        // exception here would propagate out of AIConversationShareService.ShareConversationAsync's
        // per-recipient loop and abort processing of any remaining recipients in a multi-recipient share.
        var (handler, userService, emailSender, logger) = BuildHandler();
        userService
            .Setup(x => x.GetAsync(RecipientKey))
            .ThrowsAsync(new InvalidOperationException("Database is unavailable"));
        var notification = BuildNotification();

        // Act / Assert — must complete without throwing.
        await handler.HandleAsync(notification, CancellationToken.None);
        VerifyLogged(logger, LogLevel.Error);
        emailSender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<TimeSpan?>()),
            Times.Never);
    }

    private static AIConversationShareCreatedNotification BuildNotification(string? title = "A conversation")
    {
        var conversationId = Guid.NewGuid();
        var share = new AIConversationShare
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SharedWithUserKey = RecipientKey,
            SharedByUserKey = OwnerKey,
            ScopeMode = AIConversationShareScopeMode.Live,
            DateShared = DateTime.UtcNow,
        };
        var conversation = new AIConversation
        {
            Id = conversationId,
            UserKey = OwnerKey,
            Title = title,
        };

        return new AIConversationShareCreatedNotification(share, conversation, new EventMessages());
    }

    private static void SetUpUser(Mock<IUserService> userService, Guid key, string? name, string? email)
    {
        var user = new Mock<IUser>();
        user.Setup(x => x.Key).Returns(key);
        user.Setup(x => x.Name).Returns(name);
        user.Setup(x => x.Email).Returns(email!);
        userService.Setup(x => x.GetAsync(key)).ReturnsAsync(user.Object);
    }

    private static void VerifyLogged<T>(Mock<ILogger<T>> logger, LogLevel level)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private static (AIConversationShareCreatedNotificationHandler Handler, Mock<IUserService> UserService, Mock<IEmailSender> EmailSender, Mock<ILogger<AIConversationShareCreatedNotificationHandler>> Logger) BuildHandler()
    {
        var userService = new Mock<IUserService>();
        var emailSender = new Mock<IEmailSender>();
        var hostingEnvironment = new Mock<IHostingEnvironment>();
        hostingEnvironment.Setup(x => x.ApplicationMainUrl).Returns(new Uri("https://my-site.example"));
        var ioHelper = new Mock<IIOHelper>();
        ioHelper.Setup(x => x.ResolveUrl(It.IsAny<string>())).Returns("/umbraco");
        var logger = new Mock<ILogger<AIConversationShareCreatedNotificationHandler>>();

        var handler = new AIConversationShareCreatedNotificationHandler(
            userService.Object,
            emailSender.Object,
            hostingEnvironment.Object,
            ioHelper.Object,
            logger.Object);

        return (handler, userService, emailSender, logger);
    }
}

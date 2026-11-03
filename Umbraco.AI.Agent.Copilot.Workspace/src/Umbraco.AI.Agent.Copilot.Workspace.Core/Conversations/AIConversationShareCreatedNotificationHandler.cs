using System.Net;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Agent.Copilot.Workspace.Core.Conversations;

/// <summary>
/// Reacts to <see cref="AIConversationShareCreatedNotification"/> (published only for a genuinely new
/// share row — see <see cref="IAIConversationShareService.ShareConversationAsync"/>) by emailing the
/// recipient, via core's <see cref="IEmailSender"/> pipeline — the same pipeline core's own
/// forgot-password and content-change-alert emails already go through. Lives in
/// <c>Umbraco.AI.Agent.Copilot.Workspace.Core</c> rather than the host-agnostic
/// <c>Umbraco.AI.Agent.Conversations.Core</c>, because building the "view conversation" link needs the
/// Workspace section pathname (<see cref="CopilotWorkspaceConstants.Sections.CopilotWorkspacePathname"/>) —
/// knowledge the Conversations assembly deliberately doesn't carry.
/// </summary>
internal sealed class AIConversationShareCreatedNotificationHandler
    : INotificationAsyncHandler<AIConversationShareCreatedNotification>
{
    private const string EmailType = "ShareConversationCreated";

    private readonly IUserService _userService;
    private readonly IEmailSender _emailSender;
    private readonly IHostingEnvironment _hostingEnvironment;
    private readonly IIOHelper _ioHelper;
    private readonly ILogger<AIConversationShareCreatedNotificationHandler> _logger;

    public AIConversationShareCreatedNotificationHandler(
        IUserService userService,
        IEmailSender emailSender,
        IHostingEnvironment hostingEnvironment,
        IIOHelper ioHelper,
        ILogger<AIConversationShareCreatedNotificationHandler> logger)
    {
        _userService = userService;
        _emailSender = emailSender;
        _hostingEnvironment = hostingEnvironment;
        _ioHelper = ioHelper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(AIConversationShareCreatedNotification notification, CancellationToken cancellationToken)
    {
        var share = notification.Share;

        // The whole body is wrapped — not just the SendAsync call — because
        // AIConversationShareService.ShareConversationAsync awaits PublishAsync inline inside its
        // per-recipient loop. An unhandled exception from ANY step here (user lookups, URL building)
        // would otherwise propagate out of a fire-and-forget handler, fail the owner's share request,
        // and abort processing of any remaining recipients in a multi-recipient share.
        try
        {
            IUser? recipient = await _userService.GetAsync(share.SharedWithUserKey);
            if (recipient is null || string.IsNullOrWhiteSpace(recipient.Email))
            {
                // A missing/incomplete user record shouldn't crash a domain-event handler reacting to an
                // already-committed database write — just skip the email.
                _logger.LogWarning(
                    "Could not send a share-notification email for conversation {ConversationId}: recipient user "
                    + "{RecipientUserKey} was not found or has no email address.",
                    notification.Conversation.Id,
                    share.SharedWithUserKey);
                return;
            }

            // Shouldn't normally be null — this is the authenticated owner who just performed the share —
            // but a null Name (or a missing user record) must never throw here.
            IUser? owner = await _userService.GetAsync(share.SharedByUserKey);
            var ownerName = string.IsNullOrWhiteSpace(owner?.Name) ? "A colleague" : owner.Name;
            var recipientGreetingName = GetFirstNameOrFallback(recipient.Name);
            var conversationTitle = string.IsNullOrWhiteSpace(notification.Conversation.Title)
                ? "Untitled conversation"
                : notification.Conversation.Title;

            var conversationUrl = BuildConversationUrl(share.ConversationId);

            // Subject/body copy below is illustrative only, pending product/legal review — see
            // ARCHITECTURE.md decision 5's TODO (GDPR-conscious posture, Danish-context org instructions).
            var subject = $"{ownerName} shared a conversation with you";
            var body = BuildEmailBody(recipientGreetingName, ownerName, conversationTitle, conversationUrl);

            var message = new EmailMessage(
                from: null, // Umbraco's own sending pipeline fills in the configured SMTP from-address.
                to: recipient.Email,
                subject: subject,
                body: body,
                isBodyHtml: true);

            // enableNotification: false (the default) — mirrors NotificationService's own
            // content-change-alert email, the closest analog to this one-off internal-collaboration
            // signal. The `true` callers in core (forgot-password, user-invite) are "required" security
            // emails that must still get a chance to send via a SendEmailNotification handler even
            // without SMTP configured; this isn't one of those.
            await _emailSender.SendAsync(message, EmailType, enableNotification: false, expires: null);
        }
        catch (Exception ex)
        {
            // A flaky SMTP server, misconfigured host, or any other failure along the way must never
            // surface as an unhandled exception out of a fire-and-forget handler reacting to an
            // already-committed share.
            _logger.LogError(
                ex,
                "Failed to send a share-notification email for conversation {ConversationId} to user {RecipientUserKey}.",
                notification.Conversation.Id,
                share.SharedWithUserKey);
        }
    }

    private static string GetFirstNameOrFallback(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "there";
        }

        var firstSpace = fullName.IndexOf(' ');
        return firstSpace > 0 ? fullName[..firstSpace] : fullName;
    }

    /// <summary>
    /// Builds a best-effort backoffice deep link to the shared conversation, matching the frontend's own
    /// <c>copilotWorkspaceConversationPath</c> (<c>paths.ts</c>): <c>/section/{sectionPathname}/conversation/{id}</c>,
    /// prefixed with the resolved Umbraco backoffice path. Uses the section *pathname*
    /// (<see cref="CopilotWorkspaceConstants.Sections.CopilotWorkspacePathname"/>), not the section alias —
    /// the two are distinct and only the pathname appears in backoffice URLs.
    /// </summary>
    private string BuildConversationUrl(Guid conversationId)
    {
        var umbracoPath = _ioHelper.ResolveUrl(Umbraco.Cms.Core.Constants.System.DefaultUmbracoPath).TrimEnd('/');
        var relativePath =
            $"{umbracoPath}/section/{CopilotWorkspaceConstants.Sections.CopilotWorkspacePathname}/conversation/{Uri.EscapeDataString(conversationId.ToString())}";

        if (_hostingEnvironment.ApplicationMainUrl is { } baseUrl)
        {
            return new Uri(baseUrl, relativePath).ToString();
        }

        _logger.LogWarning(
            "Could not resolve the application's base URL while building a share-notification email link "
            + "for conversation {ConversationId}; falling back to a relative link.",
            conversationId);
        return relativePath;
    }

    /// <summary>
    /// Mirrors the shell every real Umbraco HTML notification email uses (dark header strip, white
    /// 560px card, teal CTA button, plain sign-off — see <c>mailBodyHtml</c> in core's own
    /// <c>en.xml</c>), per the mockup at
    /// <c>docs/plans/share-conversations/mockups/share-notification-email.html</c>. Untrusted values
    /// (names, conversation title) are HTML-encoded before interpolation.
    /// </summary>
    private static string BuildEmailBody(string recipientGreetingName, string ownerName, string conversationTitle, string conversationUrl)
    {
        var encodedRecipientGreetingName = WebUtility.HtmlEncode(recipientGreetingName);
        var encodedOwnerName = WebUtility.HtmlEncode(ownerName);
        var encodedConversationTitle = WebUtility.HtmlEncode(conversationTitle);
        var encodedConversationUrl = WebUtility.HtmlEncode(conversationUrl);

        return $$"""
            <table width="100%" bgcolor="#1d1333" style="background:#1d1333; margin:0; padding:0; border-collapse:collapse;">
              <tr>
                <td align="center" style="padding: 36px 20px;">
                  <table width="560" style="max-width: 560px; width: 560px; margin: 0 auto; background: #FFFFFF; border-radius: 5px;">
                    <tr>
                      <td style="padding: 40px; font-family: sans-serif; font-size: 14px; line-height: 22px; color: #392F54;">
                        <h1 style="color: #392F54; font-family: sans-serif; font-weight: bold; line-height: 1.4; font-size: 24px; text-align: left; margin: 0 0 24px;">Hi {{encodedRecipientGreetingName}},</h1>
                        <p style="margin: 0 0 16px;"><strong>{{encodedOwnerName}}</strong> shared a Copilot conversation with you:</p>
                        <p style="margin: 0 0 24px; font-size: 16px; font-weight: bold;">"{{encodedConversationTitle}}"</p>
                        <table style="margin: 0 0 28px;">
                          <tr>
                            <td style="border-radius: 5px; text-align: center; background: #35C786;" align="center" bgcolor="#35C786">
                              <a href="{{encodedConversationUrl}}" target="_blank" rel="noopener" style="color: #FFFFFF; text-decoration: none; border-radius: 5px; font-family: sans-serif; font-size: 14px; font-weight: bold; display: inline-block; background: #35C786; margin: 0; padding: 12px 30px; border: 1px solid #35c786;">View conversation</a>
                            </td>
                          </tr>
                        </table>
                        <p style="margin: 0 0 28px; font-size: 13px; color: #6b6279;">You'll be able to read it — you won't be able to send messages in it. This link only works for your own Umbraco account.</p>
                        <p style="margin: 0;">Have a nice day!<br><br>Cheers from the Umbraco robot</p>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
            """;
    }
}

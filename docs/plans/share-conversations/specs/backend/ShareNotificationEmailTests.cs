// STAGED SPEC — see ShareConversationTests.cs's header for why this lives here and where it moves
// (Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit/Conversations/, as part of SC-05).

using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// STORIES.md US1 AC6–7 — the recipient is emailed when newly shared with, but not on a re-share
/// that only changes scope. Exercises the ARCHITECTURE.md decision 5 hook into
/// ShareConversationAsync: publishing Umbraco core's SendEmailNotification, not a bespoke send.
/// </summary>
public class ShareNotificationEmailTests
{
    private static readonly Guid OwnerKey = Guid.NewGuid();
    private static readonly Guid RecipientKey = Guid.NewGuid();

    #region Happy path

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task ShareConversationAsync_NewRecipient_PublishesSendEmailNotification()
    {
        // AC6 — a new share row being created publishes SendEmailNotification via the event
        // aggregator (the same mechanism core's own forgot-password/content-alert emails use), not a
        // direct SMTP call.
    }

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task ShareConversationAsync_NewRecipient_EmailNamesOwnerAndLinksToConversation()
    {
        // AC6 — the NotificationEmailModel content includes the owner's display name and a link
        // resolvable to the shared conversation.
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task ShareConversationAsync_ExistingRecipientScopeChangeOnly_DoesNotPublishSecondEmail()
    {
        // AC7 — re-sharing to someone who already has an active share (the idempotent-upsert path in
        // ShareConversationTests.ShareConversationAsync_RecipientAlreadyShared_UpdatesExistingRowInstead)
        // must not trigger a second SendEmailNotification.
    }

    #endregion
}

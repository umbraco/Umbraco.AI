// STAGED SPEC — not yet in a real test project. Move to
// Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit/Conversations/ (matching
// AIConversationServiceTruncateTests.cs's location/style: Moq + Shouldly, flat class,
// Method_Scenario_ExpectedResult naming, a BuildService() factory) as part of task SC-06.

using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// STORIES.md US1 AC7–8 — the recipient is emailed when newly shared with, but not on a re-share
/// that only changes scope. Exercises the ARCHITECTURE.md decision 5 hook into
/// ShareConversationAsync: publishing Umbraco core's SendEmailNotification, not a bespoke send.
/// </summary>
public class ShareNotificationEmailTests
{
    private static readonly Guid OwnerKey = Guid.NewGuid();
    private static readonly Guid RecipientKey = Guid.NewGuid();

    #region Happy path

    [Fact(Skip = "Pending implementation — SC-06")]
    public async Task ShareConversationAsync_NewRecipient_PublishesSendEmailNotification()
    {
        // AC7 — a new share row being created publishes SendEmailNotification via the event
        // aggregator (the same mechanism core's own forgot-password/content-alert emails use), not a
        // direct SMTP call.
    }

    [Fact(Skip = "Pending implementation — SC-06")]
    public async Task ShareConversationAsync_NewRecipient_EmailNamesOwnerAndLinksToConversation()
    {
        // AC7 — the NotificationEmailModel content includes the owner's display name and a link
        // resolvable to the shared conversation.
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation — SC-06")]
    public async Task ShareConversationAsync_ExistingRecipientScopeChangeOnly_DoesNotPublishSecondEmail()
    {
        // AC8 — re-sharing to someone who already has an active share (the idempotent-upsert path in
        // ShareConversationTests.ShareConversationAsync_RecipientAlreadyShared_UpdatesExistingRowInstead)
        // must not trigger a second SendEmailNotification.
    }

    #endregion
}

using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Published before an <see cref="AIConversation"/> is saved (created or updated). Cancelable — a handler
/// can veto the save by setting <see cref="Umbraco.Cms.Core.Notifications.CancelableNotification.Cancel"/>.
/// Message appends during a chat run are not entity saves and do not raise this notification.
/// </summary>
public sealed class AIConversationSavingNotification : AIEntitySavingNotification<AIConversation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConversationSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The conversation being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIConversationSavingNotification(AIConversation entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

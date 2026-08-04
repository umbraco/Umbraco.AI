using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Published before an <see cref="AIConversation"/> is deleted. Cancelable — a handler can veto the
/// delete by setting <see cref="Umbraco.Cms.Core.Notifications.CancelableNotification.Cancel"/>.
/// </summary>
public sealed class AIConversationDeletingNotification : AIEntityDeletingNotification<AIConversation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConversationDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the conversation being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIConversationDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

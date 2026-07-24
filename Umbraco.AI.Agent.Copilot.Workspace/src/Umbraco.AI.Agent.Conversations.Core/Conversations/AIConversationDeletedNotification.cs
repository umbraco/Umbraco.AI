using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Published after an <see cref="AIConversation"/> has been deleted. Not cancelable.
/// </summary>
public sealed class AIConversationDeletedNotification : AIEntityDeletedNotification<AIConversation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConversationDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the conversation that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AIConversationDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

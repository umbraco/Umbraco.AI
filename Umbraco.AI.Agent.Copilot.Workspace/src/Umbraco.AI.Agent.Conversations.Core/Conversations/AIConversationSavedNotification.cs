using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Published after an <see cref="AIConversation"/> has been saved (created or updated). Not cancelable.
/// </summary>
public sealed class AIConversationSavedNotification : AIEntitySavedNotification<AIConversation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConversationSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The conversation that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AIConversationSavedNotification(AIConversation entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

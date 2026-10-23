using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Published after a <em>new</em> <see cref="AIConversationShare"/> row is created for a recipient
/// (not cancelable — it has already happened). Never published when an existing recipient's active
/// share is merely updated (e.g. a scope-mode change) — see
/// <see cref="IAIConversationShareService.ShareConversationAsync"/>'s idempotent-upsert behavior.
/// </summary>
public sealed class AIConversationShareCreatedNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConversationShareCreatedNotification"/> class.
    /// </summary>
    /// <param name="share">The share row that was created.</param>
    /// <param name="conversation">The conversation the share belongs to.</param>
    /// <param name="messages">Event messages from the share operation.</param>
    public AIConversationShareCreatedNotification(AIConversationShare share, AIConversation conversation, EventMessages messages)
    {
        Share = share;
        Conversation = conversation;
        Messages = messages;
    }

    /// <summary>Gets the share row that was created.</summary>
    public AIConversationShare Share { get; }

    /// <summary>Gets the conversation the share belongs to.</summary>
    public AIConversation Conversation { get; }

    /// <summary>Gets the event messages.</summary>
    public EventMessages Messages { get; }
}

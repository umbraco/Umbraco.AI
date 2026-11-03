namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Thrown when a <see cref="AIConversationShareScopeMode.Snapshot"/> share can't be anchored to a
/// concrete message — the conversation has no messages yet, or a concurrent regenerate emptied it
/// between the count and fetch calls used to resolve the cutoff.
/// </summary>
/// <remarks>
/// Derives from <see cref="InvalidOperationException"/> so existing callers that already catch that
/// keep working. Its own type lets a future web layer tell "nothing to anchor a snapshot to" apart
/// from "conversation not found" (<see cref="AIConversationShareService"/>'s ownership check) and
/// answer with a different status code rather than a 404 or 500 — this is deliberately a rejection,
/// not a silent fall-back to a Live (uncapped) share.
/// </remarks>
internal sealed class AIConversationShareCutoffUnresolvableException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConversationShareCutoffUnresolvableException"/> class.
    /// </summary>
    /// <param name="conversationId">The conversation a Snapshot cutoff could not be resolved for.</param>
    public AIConversationShareCutoffUnresolvableException(Guid conversationId)
        : base($"Conversation '{conversationId}' has no message to anchor a Snapshot share to.")
        => ConversationId = conversationId;

    /// <summary>Gets the conversation a Snapshot cutoff could not be resolved for.</summary>
    public Guid ConversationId { get; }
}

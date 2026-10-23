namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Owner-side operations for sharing one of the acting user's own conversations with other backoffice
/// users. A separate interface from <see cref="IAIConversationService"/> — not an addition to it —
/// because that interface is public and adding new abstract members to it would be a breaking change
/// for existing implementers, and there is no sensible default-interface-method body for "share a
/// conversation" (unlike <see cref="IAIConversationService.GetLastAssistantAgentIdAsync"/>'s existing
/// fallback). Every method here enforces the same ownership boundary as the write side of
/// <see cref="IAIConversationService"/>: not-found and not-owned are deliberately indistinguishable, so
/// ownership can't be probed.
/// </summary>
public interface IAIConversationShareService
{
    /// <summary>
    /// Creates or updates an active share for each of <paramref name="recipientUserKeys"/> on one of
    /// the acting user's own conversations — idempotent: a recipient who already has an active share
    /// has their <paramref name="scopeMode"/> updated in place rather than getting a duplicate row.
    /// For <see cref="AIConversationShareScopeMode.Snapshot"/>, the cutoff message is always resolved
    /// server-side to the conversation's current last message — never accepted from the caller. For
    /// <see cref="AIConversationShareScopeMode.Live"/>, no cutoff is set. Returns the resulting list of
    /// active shares for the conversation.
    /// </summary>
    Task<IReadOnlyList<AIConversationShare>> ShareConversationAsync(
        Guid conversationId,
        IReadOnlyList<Guid> recipientUserKeys,
        AIConversationShareScopeMode scopeMode,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the active (non-revoked) shares for one of the acting user's own conversations.</summary>
    Task<IReadOnlyList<AIConversationShare>> GetConversationSharesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the active share for one recipient on one of the acting user's own conversations.
    /// Throws if there is no active share for that recipient — a second revoke is a real failure, not
    /// silently accepted.
    /// </summary>
    Task RevokeConversationShareAsync(
        Guid conversationId,
        Guid recipientUserKey,
        CancellationToken cancellationToken = default);
}

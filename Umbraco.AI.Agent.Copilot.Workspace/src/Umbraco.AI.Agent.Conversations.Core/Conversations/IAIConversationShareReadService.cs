namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Recipient-side read operations for conversations shared with the acting user. A deliberately
/// separate code path from <see cref="IAIConversationService"/>'s owner-gated reads — that interface's
/// <see cref="IAIConversationService.GetConversationAsync"/>/<see cref="IAIConversationService.GetMessagesPagedAsync"/>
/// return null/empty for anyone who isn't the conversation's owner, which is exactly wrong for a
/// recipient. Every method here resolves access through
/// <see cref="IAIConversationShareRepository"/>'s active-share lookup instead of conversation
/// ownership, and never calls into <see cref="IAIConversationService"/> at all — shared access and
/// owned access are two independently auditable paths, not one path with a widened equality check.
/// </summary>
public interface IAIConversationShareReadService
{
    /// <summary>
    /// Gets the conversation and its visible messages for an active share addressed to the acting
    /// user, or <c>null</c> when there is no active (non-revoked) share for
    /// <paramref name="conversationId"/> and the acting user — including when there is no acting user
    /// at all. For a <see cref="AIConversationShareScopeMode.Snapshot"/> share, only messages whose
    /// <see cref="AIMessage.Sequence"/> is at or before the share's cutoff message's own sequence are
    /// returned; if that cutoff message can no longer be found (e.g. a regenerate truncated past it),
    /// every message that still exists is returned instead — never an error, and never a reason to
    /// reveal more than what remains. For a
    /// <see cref="AIConversationShareScopeMode.Live"/> share, every message is returned. Attachments
    /// (<see cref="AIConversation.Resources"/>/<see cref="AIConversation.ContextIds"/>) are never
    /// filtered per-viewer — the same attachment list the owner sees is returned as-is, with no
    /// per-viewer redaction.
    /// </summary>
    Task<(AIConversation Conversation, IReadOnlyList<AIMessage> Messages)?> GetSharedConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of conversations actively shared with the acting user — the recipient's own
    /// "shared with me" list — optionally filtered by <paramref name="search"/> (title only, see
    /// <see cref="IAIConversationShareRepository.GetSharedWithUserPagedAsync"/>). There is no ownership
    /// check to apply: every result is, by definition, a conversation shared <em>with</em> the acting
    /// user. Returns an empty page when there is no acting user.
    /// </summary>
    Task<(IReadOnlyList<AISharedConversationSummary> Items, int Total)> GetSharedConversationsPagedAsync(
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default);
}

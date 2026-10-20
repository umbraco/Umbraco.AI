namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Repository for conversation share persistence. Internal implementation detail — external callers
/// go through the service layer. Deliberately separate from <see cref="IAIConversationRepository"/>:
/// shares have their own lifecycle (per-recipient revoke, a recipient-facing read path) that must
/// never be reachable through the owner's conversation read/write methods, so a bug in one can't
/// accidentally expose or corrupt the other.
/// </summary>
internal interface IAIConversationShareRepository
{
    /// <summary>
    /// Looks up the recipient's <em>active</em> row only (<see cref="AIConversationShare.DateRevoked"/>
    /// is <c>null</c>) for <see cref="AIConversationShare.ConversationId"/> +
    /// <see cref="AIConversationShare.SharedWithUserKey"/>. If one exists, updates its
    /// <see cref="AIConversationShare.ScopeMode"/>/<see cref="AIConversationShare.CutoffMessageId"/>
    /// and returns it with <c>WasCreated: false</c>. If none exists — whether this recipient has
    /// never been shared with, or was shared with and then revoked — inserts a brand-new row with a
    /// fresh <see cref="AIConversationShare.DateShared"/> and returns it with <c>WasCreated: true</c>.
    /// Re-sharing after a revoke is therefore always a new grant (its own row, the revoked row's
    /// history untouched), never a resurrection of the old one — the insert gets a fresh
    /// server-assigned id and <c>DateShared</c> regardless of what the caller's object carried. The
    /// caller uses <c>WasCreated</c> to decide whether to send the share-notification email (never
    /// sent on an update).
    /// </summary>
    Task<(AIConversationShare Share, bool WasCreated)> SaveAsync(
        AIConversationShare share,
        CancellationToken cancellationToken = default);

    /// <summary>Gets every active (non-revoked) share for a conversation — the owner's recipient list.</summary>
    Task<IReadOnlyList<AIConversationShare>> GetActiveByConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the single active (non-revoked) share for a conversation addressed to a specific viewer,
    /// or <c>null</c> if none exists — the recipient-facing read path.
    /// </summary>
    Task<AIConversationShare?> GetActiveShareAsync(
        Guid conversationId,
        Guid viewerUserKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes (sets <see cref="AIConversationShare.DateRevoked"/> on) the active share for a
    /// conversation/recipient pair. Returns <c>false</c>, distinct from a no-op success, when there is
    /// no active share for that pair — revoking an already-revoked or nonexistent share must not look
    /// like it succeeded.
    /// </summary>
    Task<bool> RevokeAsync(
        Guid conversationId,
        Guid userKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of every active (non-revoked) share where <paramref name="viewerUserKey"/> is
    /// the recipient, newest first, optionally filtered by <paramref name="search"/> — the recipient's
    /// own "shared with me" list. Each result carries the owning conversation's own summary fields
    /// inline so the caller never needs a second round-trip per row.
    /// </summary>
    /// <param name="search">
    /// Matches the shared conversation's title only — never message content. A Snapshot share hides
    /// messages after its cutoff from the recipient; matching message content here would let a search
    /// hit (or miss) leak whether a word exists past that cutoff, through a channel the cutoff is
    /// supposed to close.
    /// </param>
    Task<(IReadOnlyList<AISharedConversationSummary> Items, int Total)> GetSharedWithUserPagedAsync(
        Guid viewerUserKey,
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default);
}

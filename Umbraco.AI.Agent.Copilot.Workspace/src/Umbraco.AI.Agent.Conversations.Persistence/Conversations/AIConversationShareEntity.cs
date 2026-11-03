namespace Umbraco.AI.Agent.Conversations.Persistence.Conversations;

/// <summary>
/// EF Core entity for a grant of read-only access to one conversation for one recipient. A
/// column-for-column mirror of <c>AIConversationShare</c>. <c>UmbracoAIConversationsDbContext</c>
/// configures a unique index on (<see cref="ConversationId"/>, <see cref="SharedWithUserKey"/>)
/// filtered to active rows only, so a revoked share never blocks re-sharing with that recipient —
/// each share/revoke cycle gets its own row rather than reusing one.
/// </summary>
internal class AIConversationShareEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owning conversation id (FK, cascade delete).
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// The key (GUID) of the recipient backoffice user.
    /// </summary>
    public Guid SharedWithUserKey { get; set; }

    /// <summary>
    /// The key (GUID) of the backoffice user who owned the conversation at the moment of sharing.
    /// </summary>
    public Guid SharedByUserKey { get; set; }

    /// <summary>
    /// The owner's choice, at share time, of how much of the conversation stays visible. Stored as
    /// its integer value (<c>AIConversationShareScopeMode</c>: Snapshot = 0, Live = 1).
    /// </summary>
    public int ScopeMode { get; set; }

    /// <summary>
    /// Set only when <see cref="ScopeMode"/> is Snapshot — the id of the last message visible at
    /// share time (references <c>AIMessageEntity.Id</c>). Always <c>null</c> for Live.
    /// </summary>
    public Guid? CutoffMessageId { get; set; }

    /// <summary>
    /// When the share was created.
    /// </summary>
    public DateTime DateShared { get; set; }

    /// <summary>
    /// When the share was revoked, or <c>null</c> while still active. Soft-revoke, not a hard
    /// delete — preserves who had access to what and when.
    /// </summary>
    public DateTime? DateRevoked { get; set; }
}

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// A grant of read-only access to one conversation for one recipient (domain model). At most one
/// <em>active</em> (<see cref="DateRevoked"/> <c>null</c>) row per conversation/recipient pair —
/// re-sharing to an active recipient updates that row, but re-sharing after a revoke inserts a
/// brand-new row, leaving the revoked row as untouched history. See <c>ARCHITECTURE.md</c>'s "Why a
/// filtered unique index" section for the full rationale.
/// </summary>
public sealed class AIConversationShare
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>The shared conversation.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>The key (GUID) of the recipient backoffice user.</summary>
    public Guid SharedWithUserKey { get; set; }

    /// <summary>
    /// The key (GUID) of the backoffice user who owned the conversation at the moment of sharing.
    /// Captured explicitly rather than inferred from the conversation so the audit trail survives
    /// even if ownership rules ever change later.
    /// </summary>
    public Guid SharedByUserKey { get; set; }

    /// <summary>The owner's choice, at share time, of how much of the conversation stays visible.</summary>
    public AIConversationShareScopeMode ScopeMode { get; set; }

    /// <summary>
    /// Set only when <see cref="ScopeMode"/> is <see cref="AIConversationShareScopeMode.Snapshot"/> —
    /// the id of the last message visible at share time (references <c>AIMessage.Id</c>). Always
    /// <c>null</c> for <see cref="AIConversationShareScopeMode.Live"/> (no ceiling).
    /// </summary>
    public Guid? CutoffMessageId { get; set; }

    /// <summary>When the share was created.</summary>
    public DateTime DateShared { get; set; }

    /// <summary>
    /// When the share was revoked, or <c>null</c> while still active. Soft-revoke, not a hard
    /// delete — preserves who had access to what and when.
    /// </summary>
    public DateTime? DateRevoked { get; set; }
}

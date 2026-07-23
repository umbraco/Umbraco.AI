namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// A persisted conversation (domain model). Persistence stays M.E.AI-agnostic: messages are loaded
/// separately and carry their content as serialized strings — the <c>ConversationChatHistoryProvider</c>
/// (Phase 3) maps between these and M.E.AI <c>ChatMessage</c>s.
/// </summary>
public sealed class AIConversation
{
    /// <summary>Unique identifier (also used as the AG-UI threadId).</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Optional owning project id — an opaque grouping reference. Conversations carry this id but do not
    /// depend on the project's internals; resolving a project's contents is the host's responsibility.
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>Display title (auto-generated from the first exchange when empty).</summary>
    public string? Title { get; set; }

    /// <summary>The key (GUID) of the owning backoffice user.</summary>
    public Guid UserKey { get; set; }

    /// <summary>The agent id or alias this conversation runs (null when using "Auto").</summary>
    public string? AgentIdOrAlias { get; set; }

    /// <summary>Optional profile id override.</summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Referenced <c>AIContext</c> ids attached to <em>this conversation only</em> (the "attach a
    /// context" mechanism at conversation scope). These stack on top of the owning project's contexts
    /// at run time — they do not replace them.
    /// </summary>
    public IList<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// Resources attached to <em>this conversation only</em> (the "attach a direct resource"
    /// mechanism at conversation scope). These stack on top of the owning project's resources at run
    /// time — they do not replace them.
    /// </summary>
    public IList<AIAttachedResource> Resources { get; set; } = [];

    /// <summary>Whether the conversation is pinned.</summary>
    public bool IsPinned { get; set; }

    /// <summary>Whether the conversation is archived.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTime DateCreated { get; set; }

    /// <summary>Last modification timestamp.</summary>
    public DateTime DateModified { get; set; }

    /// <summary>Timestamp of the most recent message.</summary>
    public DateTime? LastMessageAt { get; set; }

    /// <summary>Optimistic-concurrency token for the concurrent-append reconcile.</summary>
    public int Version { get; set; } = 1;
}

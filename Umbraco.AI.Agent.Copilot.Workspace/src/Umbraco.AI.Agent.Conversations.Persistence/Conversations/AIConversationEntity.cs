namespace Umbraco.AI.Agent.Conversations.Persistence.Conversations;

/// <summary>
/// EF Core entity for a persisted conversation.
/// </summary>
internal class AIConversationEntity
{
    /// <summary>
    /// Unique identifier. Also used as the AG-UI <c>threadId</c> so uploaded files resolve stably
    /// across runs/reloads.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Optional owning project id (FK, nullable; set null when the project is deleted so conversations
    /// are orphaned rather than cascade-deleted).
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Display title (auto-generated from the first exchange when empty).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The key (GUID) of the owning backoffice user. Conversations are private per user.
    /// </summary>
    public Guid UserKey { get; set; }

    /// <summary>
    /// The agent id or alias this conversation runs (nullable when using "Auto").
    /// </summary>
    public string? AgentIdOrAlias { get; set; }

    /// <summary>
    /// Optional profile id override.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// JSON-serialized array of referenced <c>AIContext</c> ids attached to this conversation only
    /// (mirrors <c>AIProjectEntity.ContextIds</c>).
    /// </summary>
    public string? ContextIds { get; set; }

    /// <summary>
    /// Whether the conversation is pinned.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Whether the conversation is archived.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// Timestamp of the most recent message (drives sort/grouping in the sidebar).
    /// </summary>
    public DateTime? LastMessageAt { get; set; }

    /// <summary>
    /// Optimistic-concurrency token — used to detect "conversation changed under you" for the
    /// concurrent-append reconcile (interrogation B1).
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Opaque, MAF-serialized <c>AgentSession</c> state (JSON produced by
    /// <c>AIAgent.SerializeSessionAsync</c>) from the most recent run — restored into the fresh session
    /// created for the next run via <c>AIAgent.DeserializeSessionAsync</c>. Session-scoped decorators
    /// (e.g. the tool-approval-response binder) record state directly on the session object rather than
    /// in chat history, so a request handled by a brand-new session per HTTP call needs this to survive
    /// across requests. Not part of the <see cref="AIConversation"/> domain model: it is an execution
    /// detail, never shown or edited through the conversation API.
    /// </summary>
    public string? SessionStateJson { get; set; }
}

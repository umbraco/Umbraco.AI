namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Repository for conversation and message persistence. Internal implementation detail — external
/// callers go through the service layer; the <c>ConversationChatHistoryProvider</c> (same assembly)
/// uses it directly as the persistence bridge.
/// </summary>
internal interface IAIConversationRepository
{
    // --- Conversations ---

    /// <summary>Gets a conversation by id, or null.</summary>
    Task<AIConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of a user's conversations, newest activity first. Optionally filters by
    /// project, a search term (matched against title AND message content), and archived state.
    /// </summary>
    Task<(IReadOnlyList<AIConversation> Items, int Total)> GetPagedAsync(
        Guid userKey,
        int skip,
        int take,
        Guid? projectId = null,
        string? search = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the user has any conversation (archived included) attached to the given project.
    /// Used to block deletion of a project that still owns conversations.
    /// </summary>
    Task<bool> ExistsByProjectAsync(Guid userKey, Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Creates a conversation.</summary>
    Task<AIConversation> CreateAsync(AIConversation conversation, CancellationToken cancellationToken = default);

    /// <summary>Updates conversation metadata (title, pin, archive, project, profile, agent).</summary>
    Task UpdateAsync(AIConversation conversation, CancellationToken cancellationToken = default);

    /// <summary>Deletes a conversation (cascades to its messages).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the conversation's persisted MAF session-state blob (see <c>AIConversationEntity.SessionStateJson</c>),
    /// or null when the conversation doesn't exist or has never run. Deliberately separate from
    /// <see cref="GetByIdAsync"/>/<see cref="UpdateAsync"/>: it is an execution detail read/written every
    /// run, not part of the <see cref="AIConversation"/> domain model shown through the conversation API.
    /// </summary>
    Task<string?> GetSessionStateJsonAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the conversation's MAF session-state blob. A no-op when the conversation no longer exists
    /// (e.g. deleted mid-run). Does not bump <see cref="AIConversation.Version"/> or <c>DateModified</c> —
    /// this is an execution detail, not user-visible conversation metadata.
    /// </summary>
    Task SetSessionStateJsonAsync(Guid id, string? sessionStateJson, CancellationToken cancellationToken = default);

    // --- Messages ---

    /// <summary>Loads all messages for a conversation in sequence order.</summary>
    Task<IReadOnlyList<AIMessage>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the plain-text of the conversation's most recent user message, or null when it has none.
    /// Used to prompt agent auto-selection on a run that carries no inbound user message of its own
    /// (a regenerate re-runs the stored turn).
    /// </summary>
    Task<string?> GetLastUserMessageTextAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <see cref="AIMessage.AgentId"/> of the conversation's newest assistant message, or null
    /// when it has none (a new conversation, or only legacy rows with no agent ID attributed). This is
    /// Copilot Workspace's previous pick for auto agent selection.
    /// </summary>
    Task<Guid?> GetLastAssistantAgentIdAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>Loads a page of messages for a conversation in sequence order.</summary>
    Task<(IReadOnlyList<AIMessage> Items, int Total)> GetMessagesPagedAsync(
        Guid conversationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends new messages to a conversation. The repository assigns each message an authoritative,
    /// contiguous <see cref="AIMessage.Sequence"/> server-side and retries on a unique-key conflict from
    /// a concurrent writer (interrogation B1), so a streamed response is never lost. Updates the
    /// conversation's <c>LastMessageAt</c> and bumps its <c>Version</c>.
    /// </summary>
    Task AddMessagesAsync(Guid conversationId, IReadOnlyList<AIMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all messages at or after <paramref name="fromSequence"/> — used to reconcile a
    /// regenerate/edit (the server-owned <c>onTruncate</c> path).
    /// </summary>
    Task DeleteMessagesFromAsync(Guid conversationId, int fromSequence, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes everything after the conversation's last user message — the trailing assistant/tool block
    /// of the most recent turn — and returns how many messages were removed. This is the regenerate
    /// truncation: the cutoff is derived server-side because the client cannot address stored rows (its
    /// display projection drops tool/system messages, and messages created live in a run carry
    /// client-side ids that were never persisted). A conversation with no user message is left untouched.
    /// </summary>
    Task<int> DeleteMessagesAfterLastUserMessageAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rewrites stored messages in one transaction: each message in <paramref name="updates"/> has its
    /// <see cref="AIMessage.ContentJson"/> and <see cref="AIMessage.ContentText"/> replaced (matched by
    /// <see cref="AIMessage.Id"/>; <see cref="AIMessage.Sequence"/>, <see cref="AIMessage.DateCreated"/> and the
    /// token counts are kept), and every id in <paramref name="deleteIds"/> is deleted. Ids that don't belong
    /// to <paramref name="conversationId"/> are ignored, so a rewrite can never touch another conversation.
    /// Deleting leaves gaps in <see cref="AIMessage.Sequence"/>, which is safe: callers only compare and order by it.
    /// If any part fails, nothing is saved.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="updates"/> holds the same message id more than once.</exception>
    Task RewriteMessagesAsync(
        Guid conversationId,
        IReadOnlyList<AIMessage> updates,
        IReadOnlyList<Guid> deleteIds,
        CancellationToken cancellationToken = default);
}

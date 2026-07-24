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

    // --- Messages ---

    /// <summary>Loads all messages for a conversation in sequence order.</summary>
    Task<IReadOnlyList<AIMessage>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default);

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
}

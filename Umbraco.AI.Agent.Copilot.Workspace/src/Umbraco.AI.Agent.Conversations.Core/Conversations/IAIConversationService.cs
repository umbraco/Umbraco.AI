namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Service for conversation and message operations. All operations are scoped to the acting backoffice
/// user (resolved from the ambient backoffice security context): reads only return the caller's own
/// conversations, and writes are rejected for conversations the caller does not own. This is the
/// server-side ownership boundary behind the section-access authorization (F-SEC / B7).
/// </summary>
public interface IAIConversationService
{
    /// <summary>Gets one of the acting user's conversations by id, or null if missing or not owned.</summary>
    Task<AIConversation?> GetConversationAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a paged list of the acting user's conversations, newest activity first.</summary>
    Task<(IReadOnlyList<AIConversation> Items, int Total)> GetConversationsPagedAsync(
        int skip,
        int take,
        Guid? projectId = null,
        string? search = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the acting user has any conversation (archived included) in the given project.
    /// Used to block deletion of a project that still owns conversations.
    /// </summary>
    Task<bool> ConversationsExistInProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Creates a conversation owned by the acting user.</summary>
    Task<AIConversation> CreateConversationAsync(AIConversation conversation, CancellationToken cancellationToken = default);

    /// <summary>Updates one of the acting user's conversations (title, pin, archive, project, profile, agent).</summary>
    Task UpdateConversationAsync(AIConversation conversation, CancellationToken cancellationToken = default);

    /// <summary>Deletes one of the acting user's conversations and purges its uploaded files.</summary>
    Task DeleteConversationAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a page of a conversation's messages in sequence order (ownership-checked).</summary>
    Task<(IReadOnlyList<AIMessage> Items, int Total)> GetMessagesPagedAsync(
        Guid conversationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}

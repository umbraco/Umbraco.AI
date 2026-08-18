using Umbraco.AI.Agent.Core.FileStore;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Tells the file store's retention sweep that a thread id backed by a persisted conversation
/// (<c>threadId := conversationId</c>) should not age out on a fixed clock. A conversation — including
/// an archived one, which is still readable — keeps its attachments for as long as it exists; they are
/// only purged, via <c>AIConversationService.DeleteConversationAsync</c>, when the conversation itself
/// is deleted.
/// </summary>
internal sealed class ConversationFileThreadLifecycleProvider : IAIFileThreadLifecycleProvider
{
    private readonly IAIConversationRepository _repository;

    public ConversationFileThreadLifecycleProvider(IAIConversationRepository repository)
    {
        _repository = repository;
    }

    public async Task<AIFileThreadLifecycleStatus> GetStatusAsync(string threadId, CancellationToken cancellationToken = default)
    {
        // Not one of ours — a plain, non-persisted chat thread doesn't parse as a conversation id.
        if (!Guid.TryParse(threadId, out var conversationId))
        {
            return AIFileThreadLifecycleStatus.Unclaimed;
        }

        // Existence only — ownership and archived state are irrelevant here. An archived conversation is
        // still readable, so its attachments must stay alive exactly like an active one's.
        var conversation = await _repository.GetByIdAsync(conversationId, cancellationToken);
        return conversation is not null
            ? AIFileThreadLifecycleStatus.Alive
            : AIFileThreadLifecycleStatus.Gone;
    }
}

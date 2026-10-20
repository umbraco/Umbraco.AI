using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Default <see cref="IAIConversationShareReadService"/>. Precedent for injecting the internal
/// <see cref="IAIConversationRepository"/> directly from a specialized reader outside
/// <see cref="AIConversationService"/>: <c>ConversationChatHistoryProvider</c> (same assembly) already
/// does this for its own read path. Deliberately never depends on <see cref="IAIConversationService"/>
/// — that service's reads are owner-gated, which is exactly wrong for a recipient; shared access and
/// owned access are two independently auditable paths, not one path with a widened equality check.
/// </summary>
internal sealed class AIConversationShareReadService : IAIConversationShareReadService
{
    private readonly IAIConversationShareRepository _shareRepository;
    private readonly IAIConversationRepository _conversationRepository;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public AIConversationShareReadService(
        IAIConversationShareRepository shareRepository,
        IAIConversationRepository conversationRepository,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _shareRepository = shareRepository;
        _conversationRepository = conversationRepository;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    public async Task<(AIConversation Conversation, IReadOnlyList<AIMessage> Messages, AIConversationShareScopeMode ScopeMode)?> GetSharedConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var viewerUserKey = GetActingUserKeyOrNull();
        if (viewerUserKey is null)
        {
            return null;
        }

        var share = await _shareRepository.GetActiveShareAsync(conversationId, viewerUserKey.Value, cancellationToken);
        if (share is null)
        {
            return null;
        }

        // Unfiltered by ownership — calling the repository directly (not IAIConversationService) is
        // the whole point: the acting user is the recipient, never the owner.
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            // The conversation is gone (e.g. deleted by its owner after being shared) — the share row
            // is left dangling; treat it the same as "not found" rather than erroring.
            return null;
        }

        var messages = await _conversationRepository.GetMessagesAsync(conversationId, cancellationToken);
        return (conversation, ApplyCutoff(messages, share), share.ScopeMode);
    }

    public async Task<(IReadOnlyList<AISharedConversationSummary> Items, int Total)> GetSharedConversationsPagedAsync(
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var viewerUserKey = GetActingUserKeyOrNull();
        if (viewerUserKey is null)
        {
            return ([], 0);
        }

        return await _shareRepository.GetSharedWithUserPagedAsync(viewerUserKey.Value, skip, take, search, cancellationToken);
    }

    /// <summary>
    /// A <see cref="AIConversationShareScopeMode.Live"/> share has no ceiling. A
    /// <see cref="AIConversationShareScopeMode.Snapshot"/> share returns every message whose
    /// <see cref="AIMessage.Sequence"/> is at or before the cutoff message's own sequence; if the
    /// cutoff message can no longer be resolved from <paramref name="messages"/> (e.g. a regenerate
    /// deleted it), every message that still exists is returned instead.
    /// <para>
    /// This is the deliberate opposite of the fallback at share-creation time
    /// (<see cref="AIConversationShareCutoffUnresolvableException"/>): there, an unresolvable cutoff
    /// rejects the share outright rather than risk it silently reading as Live (no ceiling at all).
    /// Here, at read time, there is nothing beyond the current tail left to withhold either way — the
    /// share was already anchored to "today's messages," and those messages are exactly what's left —
    /// so returning everything that remains is the correct fallback, not a reintroduction of that same
    /// bug.
    /// </para>
    /// </summary>
    private static IReadOnlyList<AIMessage> ApplyCutoff(IReadOnlyList<AIMessage> messages, AIConversationShare share)
    {
        if (share.ScopeMode == AIConversationShareScopeMode.Live || share.CutoffMessageId is null)
        {
            return messages;
        }

        var cutoff = messages.FirstOrDefault(m => m.Id == share.CutoffMessageId.Value);
        return cutoff is null
            ? messages
            : messages.Where(m => m.Sequence <= cutoff.Sequence).ToList();
    }

    private Guid? GetActingUserKeyOrNull()
        => _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key;
}

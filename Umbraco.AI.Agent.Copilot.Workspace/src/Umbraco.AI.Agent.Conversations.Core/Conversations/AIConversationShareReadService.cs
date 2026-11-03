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
    /// <see cref="AIMessage.Sequence"/> is at or before the cutoff message's own sequence.
    /// <para>
    /// If the cutoff message can no longer be resolved from <paramref name="messages"/> (e.g. a
    /// regenerate's <c>TruncateAfterLastUserMessageAsync</c> deleted it), falling back to "every
    /// message that currently exists" is unsafe: the regenerate also reassigns fresh
    /// <see cref="AIMessage.Sequence"/> values starting from the surviving max, so a reply written
    /// long after the share — and anything the owner writes after that — would reuse sequence numbers
    /// that look like they belong before the (deleted) cutoff. The fallback is instead a time bound on
    /// the share's own <see cref="AIConversationShare.DateShared"/>: only messages that already existed
    /// at share time (<see cref="AIMessage.DateCreated"/> at or before <c>DateShared</c>) are returned.
    /// This still never errors, and still never reveals more than what existed when the share was
    /// made — it just stops trusting post-truncation sequence numbers to mean "before the cutoff."
    /// </para>
    /// <para>
    /// This is the deliberate opposite of the fallback at share-creation time
    /// (<see cref="AIConversationShareCutoffUnresolvableException"/>): there, an unresolvable cutoff
    /// rejects the share outright rather than risk it silently reading as Live (no ceiling at all).
    /// Here, at read time, the share already exists and must still honor its own frozen-at-sharing-time
    /// promise even once the anchor message is gone.
    /// </para>
    /// </summary>
    private static IReadOnlyList<AIMessage> ApplyCutoff(IReadOnlyList<AIMessage> messages, AIConversationShare share)
    {
        if (share.ScopeMode == AIConversationShareScopeMode.Live || share.CutoffMessageId is null)
        {
            return messages;
        }

        var cutoff = messages.FirstOrDefault(m => m.Id == share.CutoffMessageId.Value);
        if (cutoff is not null)
        {
            return messages.Where(m => m.Sequence <= cutoff.Sequence).ToList();
        }

        return messages.Where(m => m.DateCreated <= share.DateShared).ToList();
    }

    private Guid? GetActingUserKeyOrNull()
        => _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key;
}

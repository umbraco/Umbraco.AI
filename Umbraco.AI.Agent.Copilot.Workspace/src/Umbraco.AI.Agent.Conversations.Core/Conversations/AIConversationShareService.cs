using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Default <see cref="IAIConversationShareService"/>. A thin ownership-checking layer over
/// <see cref="IAIConversationShareRepository"/>, mirroring <see cref="AIConversationService"/>'s
/// relationship to <see cref="IAIConversationRepository"/>. Ownership is verified by calling
/// <see cref="IAIConversationService.GetConversationAsync"/> rather than querying the conversation
/// repository directly, so the not-found/not-owned check is never duplicated.
/// </summary>
internal sealed class AIConversationShareService : IAIConversationShareService
{
    private readonly IAIConversationShareRepository _shareRepository;
    private readonly IAIConversationService _conversationService;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IEventAggregator _eventAggregator;

    public AIConversationShareService(
        IAIConversationShareRepository shareRepository,
        IAIConversationService conversationService,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IEventAggregator eventAggregator)
    {
        _shareRepository = shareRepository;
        _conversationService = conversationService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _eventAggregator = eventAggregator;
    }

    public async Task<IReadOnlyList<AIConversationShare>> ShareConversationAsync(
        Guid conversationId,
        IReadOnlyList<Guid> recipientUserKeys,
        AIConversationShareScopeMode scopeMode,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetOwnedConversationOrThrowAsync(conversationId, cancellationToken);
        var sharedByUserKey = GetRequiredActingUserKey();

        if (recipientUserKeys.Any(recipientUserKey => recipientUserKey == sharedByUserKey))
        {
            throw new InvalidOperationException("Cannot share a conversation with its own owner.");
        }

        // Resolved server-side, never accepted from the caller — same posture as UserKey elsewhere in
        // this service layer. Live mode never sets a ceiling. Snapshot mode throws rather than falling
        // back to null when no message can be found to anchor to — null is exactly what a Live share's
        // cutoff looks like, so a silent fallback would widen the share instead of rejecting it.
        Guid? cutoffMessageId = scopeMode == AIConversationShareScopeMode.Snapshot
            ? await GetRequiredLastMessageIdAsync(conversationId, cancellationToken)
            : null;

        var messages = new EventMessages();

        foreach (var recipientUserKey in recipientUserKeys)
        {
            var share = new AIConversationShare
            {
                ConversationId = conversationId,
                SharedWithUserKey = recipientUserKey,
                SharedByUserKey = sharedByUserKey,
                ScopeMode = scopeMode,
                CutoffMessageId = cutoffMessageId,
            };

            var (saved, wasCreated) = await _shareRepository.SaveAsync(share, cancellationToken);

            // Only fires for a genuinely new share, not a scope-change update to an already-active
            // one — a separate notification handler decides whether to email the recipient.
            if (wasCreated)
            {
                await _eventAggregator.PublishAsync(
                    new AIConversationShareCreatedNotification(saved, conversation, messages),
                    cancellationToken);
            }
        }

        return await _shareRepository.GetActiveByConversationAsync(conversationId, cancellationToken);
    }

    public async Task<IReadOnlyList<AIConversationShare>> GetConversationSharesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        await GetOwnedConversationOrThrowAsync(conversationId, cancellationToken);
        return await _shareRepository.GetActiveByConversationAsync(conversationId, cancellationToken);
    }

    public async Task RevokeConversationShareAsync(
        Guid conversationId,
        Guid recipientUserKey,
        CancellationToken cancellationToken = default)
    {
        await GetOwnedConversationOrThrowAsync(conversationId, cancellationToken);

        var revoked = await _shareRepository.RevokeAsync(conversationId, recipientUserKey, cancellationToken);

        // A second revoke (or one for a recipient who was never shared with) is a real failure, not
        // a silently accepted no-op.
        if (!revoked)
        {
            throw new InvalidOperationException(
                $"No active share was found for conversation '{conversationId}' and user '{recipientUserKey}'.");
        }
    }

    /// <summary>
    /// Resolves the conversation's current last message id for a Snapshot cutoff. Uses the existing
    /// (ownership-checked) paged messages query rather than adding a new targeted repository method:
    /// one call for the total, a second for just the last page of one. Throws when no message can be
    /// resolved — either the conversation has no messages yet, or a concurrent regenerate emptied its
    /// tail between the two calls — rather than returning null, since null means "Live, no ceiling" in
    /// the stored data and would silently widen the share instead of rejecting it.
    /// </summary>
    private async Task<Guid> GetRequiredLastMessageIdAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var (_, total) = await _conversationService.GetMessagesPagedAsync(conversationId, 0, 0, cancellationToken);
        if (total == 0)
        {
            throw new AIConversationShareCutoffUnresolvableException(conversationId);
        }

        var (items, _) = await _conversationService.GetMessagesPagedAsync(
            conversationId, total - 1, 1, cancellationToken);
        if (items.Count == 0)
        {
            throw new AIConversationShareCutoffUnresolvableException(conversationId);
        }

        return items[0].Id;
    }

    private async Task<AIConversation> GetOwnedConversationOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetConversationAsync(id, cancellationToken);

        // Not-found and not-owned are deliberately indistinguishable so ownership can't be probed.
        return conversation
            ?? throw new InvalidOperationException($"Conversation '{id}' was not found for the acting user.");
    }

    private Guid GetRequiredActingUserKey()
        => _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key
            ?? throw new InvalidOperationException("No acting backoffice user is available for this operation.");
}

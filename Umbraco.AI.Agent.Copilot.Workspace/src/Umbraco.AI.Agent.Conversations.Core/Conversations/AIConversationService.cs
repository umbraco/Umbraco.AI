using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Default <see cref="IAIConversationService"/>. Enforces per-user ownership on top of the repository
/// and purges a conversation's uploaded files (keyed by conversation id, per the
/// <c>threadId := conversationId</c> scheme) when it is deleted.
/// </summary>
internal sealed class AIConversationService : IAIConversationService
{
    private readonly IAIConversationRepository _repository;
    private readonly IAIFileStore _fileStore;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IEventAggregator _eventAggregator;

    public AIConversationService(
        IAIConversationRepository repository,
        IAIFileStore fileStore,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IEventAggregator eventAggregator)
    {
        _repository = repository;
        _fileStore = fileStore;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _eventAggregator = eventAggregator;
    }

    public async Task<AIConversation?> GetConversationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userKey = GetActingUserKeyOrNull();
        if (userKey is null)
        {
            return null;
        }

        var conversation = await _repository.GetByIdAsync(id, cancellationToken);
        return conversation is not null && conversation.UserKey == userKey.Value ? conversation : null;
    }

    public async Task<(IReadOnlyList<AIConversation> Items, int Total)> GetConversationsPagedAsync(
        int skip,
        int take,
        Guid? projectId = null,
        string? search = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var userKey = GetActingUserKeyOrNull();
        if (userKey is null)
        {
            return ([], 0);
        }

        return await _repository.GetPagedAsync(
            userKey.Value, skip, take, projectId, search, includeArchived, cancellationToken);
    }

    public async Task<bool> ConversationsExistInProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var userKey = GetActingUserKeyOrNull();
        if (userKey is null)
        {
            return false;
        }

        return await _repository.ExistsByProjectAsync(userKey.Value, projectId, cancellationToken);
    }

    public async Task<AIConversation> CreateConversationAsync(AIConversation conversation, CancellationToken cancellationToken = default)
    {
        // The acting user always owns what they create — never trust a client-supplied UserKey.
        conversation.UserKey = GetRequiredActingUserKey();

        var messages = new EventMessages();
        var savingNotification = new AIConversationSavingNotification(conversation, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);
        if (savingNotification.Cancel)
        {
            throw new InvalidOperationException($"Conversation save cancelled: {DescribeMessages(messages)}");
        }

        var created = await _repository.CreateAsync(conversation, cancellationToken);

        var savedNotification = new AIConversationSavedNotification(created, messages).WithStateFrom(savingNotification);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return created;
    }

    public async Task UpdateConversationAsync(AIConversation conversation, CancellationToken cancellationToken = default)
    {
        var existing = await GetOwnedOrThrowAsync(conversation.Id, cancellationToken);

        // Preserve immutable ownership; the caller cannot reassign a conversation to another user.
        conversation.UserKey = existing.UserKey;

        var messages = new EventMessages();
        var savingNotification = new AIConversationSavingNotification(conversation, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);
        if (savingNotification.Cancel)
        {
            throw new InvalidOperationException($"Conversation save cancelled: {DescribeMessages(messages)}");
        }

        await _repository.UpdateAsync(conversation, cancellationToken);

        var savedNotification = new AIConversationSavedNotification(conversation, messages).WithStateFrom(savingNotification);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);
    }

    public async Task DeleteConversationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GetOwnedOrThrowAsync(id, cancellationToken);

        var messages = new EventMessages();
        var deletingNotification = new AIConversationDeletingNotification(id, messages);
        await _eventAggregator.PublishAsync(deletingNotification, cancellationToken);
        if (deletingNotification.Cancel)
        {
            throw new InvalidOperationException($"Conversation delete cancelled: {DescribeMessages(messages)}");
        }

        await _repository.DeleteAsync(id, cancellationToken);

        // Files are scoped under the conversation id (threadId := conversationId); purge them too.
        await _fileStore.CleanupThreadAsync(id.ToString(), cancellationToken);

        var deletedNotification = new AIConversationDeletedNotification(id, messages).WithStateFrom(deletingNotification);
        await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);
    }

    private static string DescribeMessages(EventMessages messages)
        => string.Join("; ", messages.GetAll().Select(m => m.Message));

    public async Task<(IReadOnlyList<AIMessage> Items, int Total)> GetMessagesPagedAsync(
        Guid conversationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        await GetOwnedOrThrowAsync(conversationId, cancellationToken);
        return await _repository.GetMessagesPagedAsync(conversationId, skip, take, cancellationToken);
    }

    public async Task<string?> GetLastUserMessageTextAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        await GetOwnedOrThrowAsync(conversationId, cancellationToken);
        return await _repository.GetLastUserMessageTextAsync(conversationId, cancellationToken);
    }

    public async Task<int> TruncateAfterLastUserMessageAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await GetOwnedOrThrowAsync(conversationId, cancellationToken);

        var messages = new EventMessages();
        var savingNotification = new AIConversationSavingNotification(conversation, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);
        if (savingNotification.Cancel)
        {
            throw new InvalidOperationException($"Conversation save cancelled: {DescribeMessages(messages)}");
        }

        var deleted = await _repository.DeleteMessagesAfterLastUserMessageAsync(conversationId, cancellationToken);

        var savedNotification = new AIConversationSavedNotification(conversation, messages).WithStateFrom(savingNotification);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return deleted;
    }

    private async Task<AIConversation> GetOwnedOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await GetConversationAsync(id, cancellationToken);

        // Not-found and not-owned are deliberately indistinguishable so ownership can't be probed.
        return conversation
            ?? throw new InvalidOperationException($"Conversation '{id}' was not found for the acting user.");
    }

    private Guid? GetActingUserKeyOrNull()
        => _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key;

    private Guid GetRequiredActingUserKey()
        => GetActingUserKeyOrNull()
            ?? throw new InvalidOperationException("No acting backoffice user is available for this operation.");
}

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Agent.Conversations.Core.Projects;

/// <summary>
/// Default <see cref="IAIProjectService"/>. Enforces per-user ownership on top of the repository.
/// </summary>
internal sealed class AIProjectService : IAIProjectService
{
    private readonly IAIProjectRepository _repository;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IEventAggregator _eventAggregator;

    public AIProjectService(
        IAIProjectRepository repository,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IEventAggregator eventAggregator)
    {
        _repository = repository;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _eventAggregator = eventAggregator;
    }

    public async Task<AIProject?> GetProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userKey = GetActingUserKeyOrNull();
        if (userKey is null)
        {
            return null;
        }

        var project = await _repository.GetByIdAsync(id, cancellationToken);
        return project is not null && project.UserKey == userKey.Value ? project : null;
    }

    public async Task<(IReadOnlyList<AIProject> Items, int Total)> GetProjectsPagedAsync(
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var userKey = GetActingUserKeyOrNull();
        if (userKey is null)
        {
            return ([], 0);
        }

        return await _repository.GetPagedAsync(userKey.Value, skip, take, search, cancellationToken);
    }

    public async Task<AIProject> SaveProjectAsync(AIProject project, CancellationToken cancellationToken = default)
    {
        var userKey = GetRequiredActingUserKey();

        if (project.Id != Guid.Empty)
        {
            var existing = await _repository.GetByIdAsync(project.Id, cancellationToken);
            if (existing is not null && existing.UserKey != userKey)
            {
                throw new InvalidOperationException($"Project '{project.Id}' was not found for the acting user.");
            }
        }

        // Never trust a client-supplied UserKey — the acting user owns what they save.
        project.UserKey = userKey;

        // Publish the (cancelable) saving notification before persisting.
        var messages = new EventMessages();
        var savingNotification = new AIProjectSavingNotification(project, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);
        if (savingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Project save cancelled: {errorMessages}");
        }

        var saved = await _repository.SaveAsync(project, cancellationToken);

        var savedNotification = new AIProjectSavedNotification(saved, messages).WithStateFrom(savingNotification);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return saved;
    }

    public async Task DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(id, cancellationToken);
        if (project is null)
        {
            // Not-found and not-owned are deliberately indistinguishable so ownership can't be probed.
            throw new InvalidOperationException($"Project '{id}' was not found for the acting user.");
        }

        // Publish the (cancelable) deleting notification. A project that still owns conversations is
        // blocked here (mirrors the connection/profile "in use" guard) so conversations are never left
        // with a dangling project reference.
        var messages = new EventMessages();
        var deletingNotification = new AIProjectDeletingNotification(id, messages);
        await _eventAggregator.PublishAsync(deletingNotification, cancellationToken);

        if (deletingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Project delete cancelled: {errorMessages}");
        }

        await _repository.DeleteAsync(id, cancellationToken);

        var deletedNotification = new AIProjectDeletedNotification(id, messages).WithStateFrom(deletingNotification);
        await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);
    }

    private Guid? GetActingUserKeyOrNull()
        => _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key;

    private Guid GetRequiredActingUserKey()
        => GetActingUserKeyOrNull()
            ?? throw new InvalidOperationException("No acting backoffice user is available for this operation.");
}

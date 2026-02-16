using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAIContextService"/>.
/// </summary>
internal sealed class AIContextService : IAIContextService
{
    private readonly IAIContextRepository _repository;
    private readonly IAIEntityVersionService _versionService;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;
    private readonly INotificationPublisher _notificationPublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextService"/> class.
    /// </summary>
    /// <param name="repository">The context repository.</param>
    /// <param name="versionService">The unified versioning service.</param>
    /// <param name="notificationPublisher">The notification publisher.</param>
    /// <param name="backOfficeSecurityAccessor">The backoffice security accessor for user tracking.</param>
    public AIContextService(
        IAIContextRepository repository,
        IAIEntityVersionService versionService,
        INotificationPublisher notificationPublisher,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _versionService = versionService;
        _notificationPublisher = notificationPublisher;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public Task<AIContext?> GetContextAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AIContext?> GetContextByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AIContext>> GetContextsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<(IEnumerable<AIContext> Items, int Total)> GetContextsPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(filter, skip, take, cancellationToken);

    /// <inheritdoc />
    public async Task<AIContext> SaveContextAsync(AIContext context, CancellationToken cancellationToken = default)
    {
        // Generate new ID if needed
        if (context.Id == Guid.Empty)
        {
            context.Id = Guid.NewGuid();
        }

        // Generate IDs for new resources
        foreach (var resource in context.Resources.Where(r => r.Id == Guid.Empty))
        {
            resource.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(context.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != context.Id)
        {
            throw new InvalidOperationException($"A context with alias '{context.Alias}' already exists.");
        }

        // Update modified timestamp
        context.DateModified = DateTime.UtcNow;

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Publish saving notification (before save)
        var messages = new EventMessages();
        var savingNotification = new AIContextSavingNotification(context, messages);
        await _notificationPublisher.PublishAsync(savingNotification, cancellationToken);

        // Check if cancelled
        if (savingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Context save cancelled: {errorMessages}");
        }

        // Check if this is an update - if so, create a version snapshot of the current state
        var existing = await _repository.GetByIdAsync(context.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        // Perform save
        var savedContext = await _repository.SaveAsync(context, userId, cancellationToken);

        // Publish saved notification (after save)
        var savedNotification = new AIContextSavedNotification(savedContext, messages)
            .WithStateFrom(savingNotification);
        await _notificationPublisher.PublishAsync(savedNotification, cancellationToken);

        return savedContext;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteContextAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Publish deleting notification (before delete)
        var messages = new EventMessages();
        var deletingNotification = new AIContextDeletingNotification(id, messages);
        await _notificationPublisher.PublishAsync(deletingNotification, cancellationToken);

        // Check if cancelled
        if (deletingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Context delete cancelled: {errorMessages}");
        }

        // Delete version history for this entity
        await _versionService.DeleteVersionsAsync(id, "Context", cancellationToken);

        // Perform delete
        var result = await _repository.DeleteAsync(id, cancellationToken);

        // Publish deleted notification (after delete)
        var deletedNotification = new AIContextDeletedNotification(id, messages)
            .WithStateFrom(deletingNotification);
        await _notificationPublisher.PublishAsync(deletedNotification, cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> ContextAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByAliasAsync(alias, cancellationToken);
        return existing is not null && (!excludeId.HasValue || existing.Id != excludeId.Value);
    }

    /// <inheritdoc />
    public Task<(IEnumerable<AIEntityVersion> Items, int Total)> GetContextVersionHistoryAsync(
        Guid contextId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionHistoryAsync(contextId, "Context", skip, take, cancellationToken);

    /// <inheritdoc />
    public Task<AIContext?> GetContextVersionSnapshotAsync(
        Guid contextId,
        int version,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionSnapshotAsync<AIContext>(contextId, version, cancellationToken);

    /// <inheritdoc />
    public async Task<AIContext> RollbackContextAsync(
        Guid contextId,
        int targetVersion,
        CancellationToken cancellationToken = default)
    {
        // Get the current context to ensure it exists
        var currentContext = await _repository.GetByIdAsync(contextId, cancellationToken);
        if (currentContext is null)
        {
            throw new InvalidOperationException($"Context with ID '{contextId}' not found.");
        }

        // Get the snapshot at the target version
        var snapshot = await _versionService.GetVersionSnapshotAsync<AIContext>(contextId, targetVersion, cancellationToken);
        if (snapshot is null)
        {
            throw new InvalidOperationException($"Version {targetVersion} not found for context '{contextId}'.");
        }

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Save the current state to version history before rolling back
        await _versionService.SaveVersionAsync(currentContext, userId, null, cancellationToken);

        // Create a new version by saving the snapshot data
        var rolledBackContext = new AIContext
        {
            Id = contextId,
            Alias = snapshot.Alias,
            Name = snapshot.Name,
            Resources = snapshot.Resources.Select(r => new AIContextResource
            {
                ResourceTypeId = r.ResourceTypeId,
                Name = r.Name,
                Description = r.Description,
                SortOrder = r.SortOrder,
                Data = r.Data,
                InjectionMode = r.InjectionMode
            }).ToList(),
        };

        return await _repository.SaveAsync(rolledBackContext, userId, cancellationToken);
    }
}

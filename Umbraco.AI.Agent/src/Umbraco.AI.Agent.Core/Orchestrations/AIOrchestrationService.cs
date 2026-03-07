using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Service implementation for orchestration management operations.
/// </summary>
internal sealed class AIOrchestrationService : IAIOrchestrationService
{
    private readonly IAIOrchestrationRepository _repository;
    private readonly IAIEntityVersionService _versionService;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;
    private readonly IEventAggregator _eventAggregator;

    public AIOrchestrationService(
        IAIOrchestrationRepository repository,
        IAIEntityVersionService versionService,
        IEventAggregator eventAggregator,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _versionService = versionService;
        _eventAggregator = eventAggregator;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public Task<AIOrchestration?> GetOrchestrationAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AIOrchestration?> GetOrchestrationByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AIOrchestration>> GetOrchestrationsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<PagedModel<AIOrchestration>> GetOrchestrationsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        string? surfaceId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, surfaceId, isActive, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AIOrchestration>> GetOrchestrationsBySurfaceAsync(string surfaceId, CancellationToken cancellationToken = default)
        => _repository.GetBySurfaceAsync(surfaceId, cancellationToken);

    /// <inheritdoc />
    public async Task<AIOrchestration> SaveOrchestrationAsync(AIOrchestration orchestration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orchestration);
        ArgumentException.ThrowIfNullOrWhiteSpace(orchestration.Alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(orchestration.Name);

        // Generate new ID if needed
        if (orchestration.Id == Guid.Empty)
        {
            orchestration.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(orchestration.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != orchestration.Id)
        {
            throw new InvalidOperationException($"An orchestration with alias '{orchestration.Alias}' already exists.");
        }

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Publish saving notification (before save)
        var messages = new EventMessages();
        var savingNotification = new AIOrchestrationSavingNotification(orchestration, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);

        // Check if cancelled
        if (savingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Orchestration save cancelled: {errorMessages}");
        }

        // Save version snapshot of existing entity before update
        var existing = await _repository.GetByIdAsync(orchestration.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        // Perform save
        var savedOrchestration = await _repository.SaveAsync(orchestration, userId, cancellationToken);

        // Publish saved notification (after save)
        var savedNotification = new AIOrchestrationSavedNotification(savedOrchestration, messages)
            .WithStateFrom(savingNotification);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return savedOrchestration;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteOrchestrationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Publish deleting notification (before delete)
        var messages = new EventMessages();
        var deletingNotification = new AIOrchestrationDeletingNotification(id, messages);
        await _eventAggregator.PublishAsync(deletingNotification, cancellationToken);

        // Check if cancelled
        if (deletingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Orchestration delete cancelled: {errorMessages}");
        }

        var deleted = await _repository.DeleteAsync(id, cancellationToken);

        if (deleted)
        {
            // Publish deleted notification (after delete)
            var deletedNotification = new AIOrchestrationDeletedNotification(id, messages)
                .WithStateFrom(deletingNotification);
            await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);
        }

        return deleted;
    }

    /// <inheritdoc />
    public Task<bool> OrchestrationAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => _repository.AliasExistsAsync(alias, excludeId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> OrchestrationsExistWithProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
        => _repository.ExistsWithProfileIdAsync(profileId, cancellationToken);
}

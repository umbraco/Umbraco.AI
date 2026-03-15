using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Default implementation of <see cref="IAIGuardrailService"/>.
/// </summary>
internal sealed class AIGuardrailService : IAIGuardrailService
{
    private readonly IAIGuardrailRepository _repository;
    private readonly IAIEntityVersionService _versionService;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;
    private readonly IEventAggregator _eventAggregator;

    public AIGuardrailService(
        IAIGuardrailRepository repository,
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
    public Task<AIGuardrail?> GetGuardrailAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AIGuardrail?> GetGuardrailByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AIGuardrail>> GetGuardrailsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<(IEnumerable<AIGuardrail> Items, int Total)> GetGuardrailsPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(filter, skip, take, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AIGuardrail>> GetGuardrailsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        => _repository.GetByIdsAsync(ids, cancellationToken);

    /// <inheritdoc />
    public async Task<AIGuardrail> SaveGuardrailAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default)
    {
        // Generate new ID if needed
        if (guardrail.Id == Guid.Empty)
        {
            guardrail.Id = Guid.NewGuid();
        }

        // Generate IDs for new rules
        foreach (var rule in guardrail.Rules.Where(r => r.Id == Guid.Empty))
        {
            rule.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(guardrail.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != guardrail.Id)
        {
            throw new InvalidOperationException($"A guardrail with alias '{guardrail.Alias}' already exists.");
        }

        // Update modified timestamp
        guardrail.DateModified = DateTime.UtcNow;

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Publish saving notification (before save)
        var messages = new EventMessages();
        var savingNotification = new AIGuardrailSavingNotification(guardrail, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);

        // Check if cancelled
        if (savingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Guardrail save cancelled: {errorMessages}");
        }

        // Check if this is an update - if so, create a version snapshot of the current state
        var existing = await _repository.GetByIdAsync(guardrail.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        // Perform save
        var savedGuardrail = await _repository.SaveAsync(guardrail, userId, cancellationToken);

        // Publish saved notification (after save)
        var savedNotification = new AIGuardrailSavedNotification(savedGuardrail, messages)
            .WithStateFrom(savingNotification);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return savedGuardrail;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteGuardrailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Publish deleting notification (before delete)
        var messages = new EventMessages();
        var deletingNotification = new AIGuardrailDeletingNotification(id, messages);
        await _eventAggregator.PublishAsync(deletingNotification, cancellationToken);

        // Check if cancelled
        if (deletingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Guardrail delete cancelled: {errorMessages}");
        }

        // Delete version history for this entity
        await _versionService.DeleteVersionsAsync(id, "Guardrail", cancellationToken);

        // Perform delete
        var result = await _repository.DeleteAsync(id, cancellationToken);

        // Publish deleted notification (after delete)
        var deletedNotification = new AIGuardrailDeletedNotification(id, messages)
            .WithStateFrom(deletingNotification);
        await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> GuardrailAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByAliasAsync(alias, cancellationToken);
        return existing is not null && (!excludeId.HasValue || existing.Id != excludeId.Value);
    }

    /// <inheritdoc />
    public Task<(IEnumerable<AIEntityVersion> Items, int Total)> GetGuardrailVersionHistoryAsync(
        Guid guardrailId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionHistoryAsync(guardrailId, "Guardrail", skip, take, cancellationToken);

    /// <inheritdoc />
    public Task<AIGuardrail?> GetGuardrailVersionSnapshotAsync(
        Guid guardrailId,
        int version,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionSnapshotAsync<AIGuardrail>(guardrailId, version, cancellationToken);

    /// <inheritdoc />
    public async Task<AIGuardrail> RollbackGuardrailAsync(
        Guid guardrailId,
        int targetVersion,
        CancellationToken cancellationToken = default)
    {
        var currentGuardrail = await _repository.GetByIdAsync(guardrailId, cancellationToken);
        if (currentGuardrail is null)
        {
            throw new InvalidOperationException($"Guardrail with ID '{guardrailId}' not found.");
        }

        var snapshot = await _versionService.GetVersionSnapshotAsync<AIGuardrail>(guardrailId, targetVersion, cancellationToken);
        if (snapshot is null)
        {
            throw new InvalidOperationException($"Version {targetVersion} not found for guardrail '{guardrailId}'.");
        }

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Publish rolling back notification (before rollback)
        var messages = new EventMessages();
        var rollingBackNotification = new AIGuardrailRollingBackNotification(guardrailId, targetVersion, messages);
        await _eventAggregator.PublishAsync(rollingBackNotification, cancellationToken);

        // Check if cancelled
        if (rollingBackNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Guardrail rollback cancelled: {errorMessages}");
        }

        // Save the current state to version history before rolling back
        await _versionService.SaveVersionAsync(currentGuardrail, userId, null, cancellationToken);

        // Create a new version by saving the snapshot data
        var rolledBackGuardrail = new AIGuardrail
        {
            Id = guardrailId,
            Alias = snapshot.Alias,
            Name = snapshot.Name,
            Rules = snapshot.Rules.Select(r => new AIGuardrailRule
            {
                EvaluatorId = r.EvaluatorId,
                Name = r.Name,
                Phase = r.Phase,
                Action = r.Action,
                Config = r.Config,
                SortOrder = r.SortOrder,
            }).ToList(),
        };

        var savedGuardrail = await _repository.SaveAsync(rolledBackGuardrail, userId, cancellationToken);

        // Publish rolled back notification (after rollback)
        var rolledBackNotification = new AIGuardrailRolledBackNotification(savedGuardrail, targetVersion, messages)
            .WithStateFrom(rollingBackNotification);
        await _eventAggregator.PublishAsync(rolledBackNotification, cancellationToken);

        return savedGuardrail;
    }
}

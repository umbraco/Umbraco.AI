using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Core.Profiles;

internal sealed class AIProfileService : IAIProfileService
{
    private readonly IAIProfileRepository _repository;
    private readonly IAISettingsService _settingsService;
    private readonly AIOptions _options;
    private readonly IAIEntityVersionService _versionService;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;
    private readonly IEventAggregator _eventAggregator;

    public AIProfileService(
        IAIProfileRepository repository,
        IAISettingsService settingsService,
        IOptions<AIOptions> options,
        IAIEntityVersionService versionService,
        IEventAggregator eventAggregator,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _settingsService = settingsService;
        _options = options.Value;
        _versionService = versionService;
        _eventAggregator = eventAggregator;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    public async Task<AIProfile?> GetProfileAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(id, cancellationToken);

    public async Task<AIProfile?> GetProfileByAliasAsync(
        string alias,
        CancellationToken cancellationToken = default)
        => await _repository.GetByAliasAsync(alias, cancellationToken);

    public async Task<IEnumerable<AIProfile>> GetAllProfilesAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);

    public async Task<IEnumerable<AIProfile>> GetProfilesAsync(
        AICapability capability,
        CancellationToken cancellationToken = default)
        => await _repository.GetByCapability(capability, cancellationToken);

    public Task<(IEnumerable<AIProfile> Items, int Total)> GetProfilesPagedAsync(
        string? filter = null,
        AICapability? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(filter, capability, skip, take, cancellationToken);

    public async Task<AIProfile> GetDefaultProfileAsync(
        AICapability capability,
        CancellationToken cancellationToken = default)
    {
        // 1. Try database settings first
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        var profileId = capability switch
        {
            AICapability.Chat => settings.DefaultChatProfileId,
            AICapability.Embedding => settings.DefaultEmbeddingProfileId,
            _ => throw new NotSupportedException($"AI capability '{capability}' is not supported.")
        };

        if (profileId.HasValue)
        {
            var profile = await _repository.GetByIdAsync(profileId.Value, cancellationToken);
            if (profile is not null)
            {
                return profile;
            }
        }

        // 2. Fall back to config-based alias
        var defaultProfileAlias = capability switch
        {
            AICapability.Chat => _options.DefaultChatProfileAlias,
            AICapability.Embedding => _options.DefaultEmbeddingProfileAlias,
            _ => null
        };

        if (defaultProfileAlias is null)
        {
            throw new InvalidOperationException($"Default {capability} profile is not configured.");
        }

        var profileByAlias = await _repository.GetByAliasAsync(defaultProfileAlias, cancellationToken);
        if (profileByAlias is null)
        {
            throw new InvalidOperationException($"Default {capability} profile with alias '{defaultProfileAlias}' not found.");
        }

        return profileByAlias;
    }

    public async Task<AIProfile> SaveProfileAsync(
        AIProfile profile,
        CancellationToken cancellationToken = default)
    {
        // Generate new ID if needed
        if (profile.Id == Guid.Empty)
        {
            profile.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(profile.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != profile.Id)
        {
            throw new InvalidOperationException($"A profile with alias '{profile.Alias}' already exists.");
        }

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Publish saving notification (before save)
        var messages = new EventMessages();
        var savingNotification = new AIProfileSavingNotification(profile, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);

        // Check if cancelled
        if (savingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Profile save cancelled: {errorMessages}");
        }

        // Check if this is an update - if so, create a version snapshot of the current state
        var existing = await _repository.GetByIdAsync(profile.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        // Perform save
        var savedProfile = await _repository.SaveAsync(profile, userId, cancellationToken);

        // Publish saved notification (after save)
        var savedNotification = new AIProfileSavedNotification(savedProfile, messages)
            .WithStateFrom(savingNotification);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return savedProfile;
    }

    public async Task<bool> DeleteProfileAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Publish deleting notification (before delete)
        var messages = new EventMessages();
        var deletingNotification = new AIProfileDeletingNotification(id, messages);
        await _eventAggregator.PublishAsync(deletingNotification, cancellationToken);

        // Check if cancelled
        if (deletingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Profile delete cancelled: {errorMessages}");
        }

        // Delete version history for this entity
        await _versionService.DeleteVersionsAsync(id, "Profile", cancellationToken);

        // Perform delete
        var result = await _repository.DeleteAsync(id, cancellationToken);

        // Publish deleted notification (after delete)
        var deletedNotification = new AIProfileDeletedNotification(id, messages)
            .WithStateFrom(deletingNotification);
        await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);

        return result;
    }

    public Task<(IEnumerable<AIEntityVersion> Items, int Total)> GetProfileVersionHistoryAsync(
        Guid profileId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionHistoryAsync(profileId, "Profile", skip, take, cancellationToken);

    public Task<AIProfile?> GetProfileVersionSnapshotAsync(
        Guid profileId,
        int version,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionSnapshotAsync<AIProfile>(profileId, version, cancellationToken);

    public async Task<AIProfile> RollbackProfileAsync(
        Guid profileId,
        int targetVersion,
        CancellationToken cancellationToken = default)
    {
        // Get the current profile to ensure it exists
        var currentProfile = await _repository.GetByIdAsync(profileId, cancellationToken);
        if (currentProfile is null)
        {
            throw new InvalidOperationException($"Profile with ID '{profileId}' not found.");
        }

        // Get the snapshot at the target version
        var snapshot = await _versionService.GetVersionSnapshotAsync<AIProfile>(profileId, targetVersion, cancellationToken);
        if (snapshot is null)
        {
            throw new InvalidOperationException($"Version {targetVersion} not found for profile '{profileId}'.");
        }

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Publish rolling back notification (before rollback)
        var messages = new EventMessages();
        var rollingBackNotification = new AIProfileRollingBackNotification(profileId, targetVersion, messages);
        await _eventAggregator.PublishAsync(rollingBackNotification, cancellationToken);

        // Check if cancelled
        if (rollingBackNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Profile rollback cancelled: {errorMessages}");
        }

        // Save the current state to version history before rolling back
        await _versionService.SaveVersionAsync(currentProfile, userId, null, cancellationToken);

        // Create a new version by saving the snapshot data
        // We need to preserve the ID and update the dates appropriately
        var rolledBackProfile = new AIProfile
        {
            Id = profileId,
            Alias = snapshot.Alias,
            Name = snapshot.Name,
            Capability = snapshot.Capability,
            ConnectionId = snapshot.ConnectionId,
            Model = snapshot.Model,
            Settings = snapshot.Settings,
            Tags = snapshot.Tags,
            // The repository will handle version increment and dates
        };

        var savedProfile = await _repository.SaveAsync(rolledBackProfile, userId, cancellationToken);

        // Publish rolled back notification (after rollback)
        var rolledBackNotification = new AIProfileRolledBackNotification(savedProfile, targetVersion, messages)
            .WithStateFrom(rollingBackNotification);
        await _eventAggregator.PublishAsync(rolledBackNotification, cancellationToken);

        return savedProfile;
    }

    /// <inheritdoc />
    public async Task<bool> ProfileAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByAliasAsync(alias, cancellationToken);
        return existing is not null && (!excludeId.HasValue || existing.Id != excludeId.Value);
    }
}

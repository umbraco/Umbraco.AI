using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Versioning;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Profiles;

internal sealed class AiProfileService : IAiProfileService
{
    private readonly IAiProfileRepository _repository;
    private readonly AiOptions _options;
    private readonly IAiEntityVersionService _versionService;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;

    public AiProfileService(
        IAiProfileRepository repository,
        IOptions<AiOptions> options,
        IAiEntityVersionService versionService,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _options = options.Value;
        _versionService = versionService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    public async Task<AiProfile?> GetProfileAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(id, cancellationToken);

    public async Task<AiProfile?> GetProfileByAliasAsync(
        string alias,
        CancellationToken cancellationToken = default)
        => await _repository.GetByAliasAsync(alias, cancellationToken);

    public async Task<IEnumerable<AiProfile>> GetAllProfilesAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);

    public async Task<IEnumerable<AiProfile>> GetProfilesAsync(
        AiCapability capability,
        CancellationToken cancellationToken = default)
        => await _repository.GetByCapability(capability, cancellationToken);

    public Task<(IEnumerable<AiProfile> Items, int Total)> GetProfilesPagedAsync(
        string? filter = null,
        AiCapability? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(filter, capability, skip, take, cancellationToken);

    public async Task<AiProfile> GetDefaultProfileAsync(
        AiCapability capability,
        CancellationToken cancellationToken = default)
    {
        var defaultProfileAlias = capability switch
        {
            AiCapability.Chat => _options.DefaultChatProfileAlias,
            AiCapability.Embedding => _options.DefaultEmbeddingProfileAlias,
            _ => throw new NotSupportedException($"AI capability '{capability}' is not supported.")
        };

        if (defaultProfileAlias is null)
        {
            throw new InvalidOperationException($"Default {capability} profile alias is not configured.");
        }

        var profile = await _repository.GetByAliasAsync(defaultProfileAlias, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException($"Default {capability} profile with alias '{defaultProfileAlias}' not found.");
        }

        return profile;
    }

    public async Task<AiProfile> SaveProfileAsync(
        AiProfile profile,
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

        // Check if this is an update - if so, create a version snapshot of the current state
        var existing = await _repository.GetByIdAsync(profile.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        return await _repository.SaveAsync(profile, userId, cancellationToken);
    }

    public async Task<bool> DeleteProfileAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Delete version history for this entity
        await _versionService.DeleteVersionsAsync(id, "Profile", cancellationToken);

        return await _repository.DeleteAsync(id, cancellationToken);
    }

    public Task<(IEnumerable<AiEntityVersion> Items, int Total)> GetProfileVersionHistoryAsync(
        Guid profileId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionHistoryAsync(profileId, "Profile", skip, take, cancellationToken);

    public Task<AiProfile?> GetProfileVersionSnapshotAsync(
        Guid profileId,
        int version,
        CancellationToken cancellationToken = default)
        => _versionService.GetVersionSnapshotAsync<AiProfile>(profileId, version, cancellationToken);

    public async Task<AiProfile> RollbackProfileAsync(
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
        var snapshot = await _versionService.GetVersionSnapshotAsync<AiProfile>(profileId, targetVersion, cancellationToken);
        if (snapshot is null)
        {
            throw new InvalidOperationException($"Version {targetVersion} not found for profile '{profileId}'.");
        }

        // Create a new version by saving the snapshot data
        // We need to preserve the ID and update the dates appropriately
        var rolledBackProfile = new AiProfile
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

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;
        return await _repository.SaveAsync(rolledBackProfile, userId, cancellationToken);
    }
}

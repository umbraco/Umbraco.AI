using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Settings;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Profiles;

internal sealed class AiProfileService : IAiProfileService
{
    private readonly IAiProfileRepository _repository;
    private readonly IAiSettingsService _settingsService;
    private readonly AiOptions _options;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;

    public AiProfileService(
        IAiProfileRepository repository,
        IAiSettingsService settingsService,
        IOptions<AiOptions> options,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _settingsService = settingsService;
        _options = options.Value;
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
        // 1. Try database settings first
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        var profileId = capability switch
        {
            AiCapability.Chat => settings.DefaultChatProfileId,
            AiCapability.Embedding => settings.DefaultEmbeddingProfileId,
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
            AiCapability.Chat => _options.DefaultChatProfileAlias,
            AiCapability.Embedding => _options.DefaultEmbeddingProfileAlias,
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

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Id;
        return await _repository.SaveAsync(profile, userId, cancellationToken);
    }

    public async Task<bool> DeleteProfileAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await _repository.DeleteAsync(id, cancellationToken);

    public Task<IEnumerable<AiEntityVersion>> GetProfileVersionHistoryAsync(
        Guid profileId,
        int? limit = null,
        CancellationToken cancellationToken = default)
        => _repository.GetVersionHistoryAsync(profileId, limit, cancellationToken);

    public Task<AiProfile?> GetProfileVersionSnapshotAsync(
        Guid profileId,
        int version,
        CancellationToken cancellationToken = default)
        => _repository.GetVersionSnapshotAsync(profileId, version, cancellationToken);
}

using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Profiles;

internal sealed class AiProfileService : IAiProfileService
{
    private readonly IAiProfileRepository _repository;
    private readonly AiOptions _options;

    public AiProfileService(IAiProfileRepository repository,
        IOptions<AiOptions> options)
    {
        _repository = repository;
        _options = options.Value;
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
        
        return await _repository.SaveAsync(profile, cancellationToken);
    }

    public async Task<bool> DeleteProfileAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await _repository.DeleteAsync(id, cancellationToken);
}
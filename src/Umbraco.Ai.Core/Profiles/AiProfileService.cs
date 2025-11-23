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

    public async Task<IEnumerable<AiProfile>> GetProfilesAsync(AiCapability capability, CancellationToken cancellationToken = default)
        => await _repository.GetByCapability(capability, cancellationToken);

    public async Task<AiProfile> GetDefaultProfileAsync(AiCapability capability,
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
}
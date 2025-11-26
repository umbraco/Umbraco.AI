using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Persistence.Entities;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the AI profile repository.
/// </summary>
internal class EfCoreAiProfileRepository : IAiProfileRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiProfileRepository"/>.
    /// </summary>
    public EfCoreAiProfileRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc />
    public async Task<AiProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiProfileEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Profiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AiProfile?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        // Case-insensitive alias lookup
        AiProfileEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Profiles.FirstOrDefaultAsync(
                p => p.Alias.ToLower() == alias.ToLower(),
                cancellationToken));

        scope.Complete();
        return entity is null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiProfileEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Profiles.ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiProfile>> GetByCapability(AiCapability capability, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        int capabilityValue = (int)capability;
        List<AiProfileEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Profiles
                .Where(p => p.Capability == capabilityValue)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<AiProfile> SaveAsync(AiProfile profile, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<object?>(async db =>
        {
            AiProfileEntity? existing = await db.Profiles.FindAsync([profile.Id], cancellationToken);

            if (existing is null)
            {
                AiProfileEntity newEntity = MapToEntity(profile);
                db.Profiles.Add(newEntity);
            }
            else
            {
                UpdateEntity(existing, profile);
            }

            await db.SaveChangesAsync(cancellationToken);
            return null;
        });

        scope.Complete();
        return profile;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AiProfileEntity? entity = await db.Profiles.FindAsync([id], cancellationToken);
            if (entity is null)
            {
                return false;
            }

            db.Profiles.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }

    private static AiProfile MapToDomain(AiProfileEntity entity)
    {
        IReadOnlyList<string> tags = Array.Empty<string>();
        if (!string.IsNullOrEmpty(entity.TagsJson))
        {
            tags = JsonSerializer.Deserialize<string[]>(entity.TagsJson) ?? Array.Empty<string>();
        }

        return new AiProfile
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Capability = (AiCapability)entity.Capability,
            Model = new AiModelRef(entity.ProviderId, entity.ModelId),
            ConnectionId = entity.ConnectionId,
            Temperature = entity.Temperature,
            MaxTokens = entity.MaxTokens,
            SystemPromptTemplate = entity.SystemPromptTemplate,
            Tags = tags
        };
    }

    private static AiProfileEntity MapToEntity(AiProfile profile)
    {
        return new AiProfileEntity
        {
            Id = profile.Id,
            Alias = profile.Alias,
            Name = profile.Name,
            Capability = (int)profile.Capability,
            ProviderId = profile.Model.ProviderId,
            ModelId = profile.Model.ModelId,
            ConnectionId = profile.ConnectionId,
            Temperature = profile.Temperature,
            MaxTokens = profile.MaxTokens,
            SystemPromptTemplate = profile.SystemPromptTemplate,
            TagsJson = profile.Tags.Count > 0 ? JsonSerializer.Serialize(profile.Tags) : null
        };
    }

    private static void UpdateEntity(AiProfileEntity entity, AiProfile profile)
    {
        entity.Alias = profile.Alias;
        entity.Name = profile.Name;
        entity.Capability = (int)profile.Capability;
        entity.ProviderId = profile.Model.ProviderId;
        entity.ModelId = profile.Model.ModelId;
        entity.ConnectionId = profile.ConnectionId;
        entity.Temperature = profile.Temperature;
        entity.MaxTokens = profile.MaxTokens;
        entity.SystemPromptTemplate = profile.SystemPromptTemplate;
        entity.TagsJson = profile.Tags.Count > 0 ? JsonSerializer.Serialize(profile.Tags) : null;
    }
}

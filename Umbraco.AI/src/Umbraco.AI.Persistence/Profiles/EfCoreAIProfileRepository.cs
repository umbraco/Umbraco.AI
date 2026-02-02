using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.Profiles;

/// <summary>
/// EF Core implementation of the AI profile repository.
/// </summary>
internal class EfCoreAiProfileRepository : IAIProfileRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="EfCoreAiProfileRepository"/>.
    /// </summary>
    public EfCoreAiProfileRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AIProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIProfileEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Profiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AIProfileFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AIProfile?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        // Case-insensitive alias lookup
        AIProfileEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Profiles.FirstOrDefaultAsync(
                p => p.Alias.ToLower() == alias.ToLower(),
                cancellationToken));

        scope.Complete();
        return entity is null ? null : AIProfileFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIProfileEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Profiles.ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AIProfileFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIProfile>> GetByCapability(AICapability capability, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        int capabilityValue = (int)capability;
        List<AIProfileEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Profiles
                .Where(p => p.Capability == capabilityValue)
                .ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AIProfileFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AIProfile> Items, int Total)> GetPagedAsync(
        string? filter = null,
        AICapability? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIProfileEntity> query = db.Profiles;

            // Apply capability filter
            if (capability.HasValue)
            {
                int capabilityValue = (int)capability.Value;
                query = query.Where(p => p.Capability == capabilityValue);
            }

            // Apply name filter (case-insensitive contains)
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(p => p.Name.ToLower().Contains(filter.ToLower()));
            }

            // Get total count before pagination
            int total = await query.CountAsync(cancellationToken);

            // Apply pagination and get items
            List<AIProfileEntity> items = await query
                .OrderBy(p => p.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AIProfileFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<AIProfile> SaveAsync(AIProfile profile, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var savedProfile = await scope.ExecuteWithContextAsync(async db =>
        {
            AIProfileEntity? existing = await db.Profiles.FindAsync([profile.Id], cancellationToken);

            if (existing is null)
            {
                // New profile - set version and user IDs on domain model before mapping
                profile.Version = 1;
                profile.DateModified = DateTime.UtcNow;
                profile.CreatedByUserId = userId;
                profile.ModifiedByUserId = userId;

                AIProfileEntity newEntity = AIProfileFactory.BuildEntity(profile);
                db.Profiles.Add(newEntity);
            }
            else
            {
                // Increment version, update timestamps, and set ModifiedByUserId on domain model
                // Note: Version snapshots are handled by the unified versioning service at the service layer
                profile.Version = existing.Version + 1;
                profile.DateModified = DateTime.UtcNow;
                profile.ModifiedByUserId = userId;

                AIProfileFactory.UpdateEntity(existing, profile);
            }

            await db.SaveChangesAsync(cancellationToken);
            return profile;
        });

        scope.Complete();
        return savedProfile;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AIProfileEntity? entity = await db.Profiles.FindAsync([id], cancellationToken);
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
}

using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Profiles;

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
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AiProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiProfileEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Profiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AiProfileFactory.BuildDomain(entity);
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
        return entity is null ? null : AiProfileFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        List<AiProfileEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Profiles.ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(AiProfileFactory.BuildDomain);
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
        return entities.Select(AiProfileFactory.BuildDomain);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<AiProfile> Items, int Total)> GetPagedAsync(
        string? filter = null,
        AiCapability? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiProfileEntity> query = db.Profiles;

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
            List<AiProfileEntity> items = await query
                .OrderBy(p => p.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AiProfileFactory.BuildDomain), result.total);
    }

    /// <inheritdoc />
    public async Task<AiProfile> SaveAsync(AiProfile profile, int? userId = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var savedProfile = await scope.ExecuteWithContextAsync(async db =>
        {
            AiProfileEntity? existing = await db.Profiles.FindAsync([profile.Id], cancellationToken);

            if (existing is null)
            {
                // New profile - set version and user IDs on domain model before mapping
                profile.Version = 1;
                profile.DateModified = DateTime.UtcNow;
                profile.CreatedByUserId = userId;
                profile.ModifiedByUserId = userId;

                AiProfileEntity newEntity = AiProfileFactory.BuildEntity(profile);
                db.Profiles.Add(newEntity);
            }
            else
            {
                // Existing profile - create version snapshot of current state before updating
                var existingDomain = AiProfileFactory.BuildDomain(existing);
                var versionEntity = new AiProfileVersionEntity
                {
                    Id = Guid.NewGuid(),
                    ProfileId = existing.Id,
                    Version = existing.Version,
                    Snapshot = AiProfileFactory.CreateSnapshot(existingDomain),
                    DateCreated = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                db.ProfileVersions.Add(versionEntity);

                // Increment version, update timestamps, and set ModifiedByUserId on domain model
                profile.Version = existing.Version + 1;
                profile.DateModified = DateTime.UtcNow;
                profile.ModifiedByUserId = userId;

                AiProfileFactory.UpdateEntity(existing, profile);
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

    /// <inheritdoc />
    public async Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid profileId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiProfileVersionEntity> query = db.ProfileVersions
                .Where(v => v.ProfileId == profileId)
                .OrderByDescending(v => v.Version);

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            return await query.ToListAsync(cancellationToken);
        });

        scope.Complete();

        return entities.Select(e => new AiEntityVersion
        {
            Id = e.Id,
            EntityId = e.ProfileId,
            Version = e.Version,
            Snapshot = e.Snapshot,
            DateCreated = e.DateCreated,
            CreatedByUserId = e.CreatedByUserId,
            ChangeDescription = e.ChangeDescription
        });
    }

    /// <inheritdoc />
    public async Task<AiProfile?> GetVersionSnapshotAsync(
        Guid profileId,
        int version,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.ProfileVersions
                .FirstOrDefaultAsync(v => v.ProfileId == profileId && v.Version == version, cancellationToken));

        scope.Complete();

        if (entity is null || string.IsNullOrEmpty(entity.Snapshot))
        {
            return null;
        }

        return AiProfileFactory.BuildDomainFromSnapshot(entity.Snapshot);
    }
}

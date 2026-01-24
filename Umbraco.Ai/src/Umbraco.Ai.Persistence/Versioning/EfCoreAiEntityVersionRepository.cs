using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Versioning;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Persistence.Versioning;

/// <summary>
/// EF Core implementation of <see cref="IAiEntityVersionRepository"/>.
/// </summary>
internal sealed class EfCoreAiEntityVersionRepository : IAiEntityVersionRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreAiEntityVersionRepository"/> class.
    /// </summary>
    /// <param name="scopeProvider">The EF Core scope provider.</param>
    public EfCoreAiEntityVersionRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid entityId,
        string entityType,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
        {
            var query = db.EntityVersions
                .Where(v => v.EntityId == entityId && v.EntityType == entityType)
                .OrderByDescending(v => v.Version);

            var limitedQuery = limit.HasValue ? query.Take(limit.Value) : query;

            return await limitedQuery.ToListAsync(cancellationToken);
        });

        scope.Complete();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<AiEntityVersion?> GetVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var entity = await scope.ExecuteWithContextAsync(async db =>
            await db.EntityVersions
                .FirstOrDefaultAsync(
                    v => v.EntityId == entityId && v.EntityType == entityType && v.Version == version,
                    cancellationToken));

        scope.Complete();
        return entity is null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task SaveVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        string snapshot,
        Guid? userId,
        string? changeDescription,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = new AiEntityVersionEntity
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                EntityType = entityType,
                Version = version,
                Snapshot = snapshot,
                DateCreated = DateTime.UtcNow,
                CreatedByUserId = userId,
                ChangeDescription = changeDescription
            };

            db.EntityVersions.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return entity;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task DeleteVersionsAsync(
        Guid entityId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
            await db.EntityVersions
                .Where(v => v.EntityId == entityId && v.EntityType == entityType)
                .ExecuteDeleteAsync(cancellationToken));

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task<int> DeleteVersionsOlderThanAsync(
        DateTime threshold,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var deleted = await scope.ExecuteWithContextAsync(async db =>
            await db.EntityVersions
                .Where(v => v.DateCreated < threshold)
                .ExecuteDeleteAsync(cancellationToken));

        scope.Complete();
        return deleted;
    }

    /// <inheritdoc />
    public async Task<int> DeleteExcessVersionsAsync(
        int maxVersionsPerEntity,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            // Get IDs of versions to delete using a subquery approach:
            // For each (EntityId, EntityType) group, find versions that are beyond the max count
            var versionsToDelete = await db.EntityVersions
                .GroupBy(v => new { v.EntityId, v.EntityType })
                .SelectMany(g => g
                    .OrderByDescending(v => v.Version)
                    .Skip(maxVersionsPerEntity)
                    .Select(v => v.Id))
                .ToListAsync(cancellationToken);

            if (versionsToDelete.Count == 0)
            {
                return 0;
            }

            // Delete the identified versions
            return await db.EntityVersions
                .Where(v => versionsToDelete.Contains(v.Id))
                .ExecuteDeleteAsync(cancellationToken);
        });

        scope.Complete();
        return deleted;
    }

    /// <inheritdoc />
    public async Task<int> GetVersionCountAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var count = await scope.ExecuteWithContextAsync(async db =>
            await db.EntityVersions.CountAsync(cancellationToken));

        scope.Complete();
        return count;
    }

    private static AiEntityVersion MapToDomain(AiEntityVersionEntity entity)
    {
        return new AiEntityVersion
        {
            Id = entity.Id,
            EntityId = entity.EntityId,
            EntityType = entity.EntityType,
            Version = entity.Version,
            Snapshot = entity.Snapshot,
            DateCreated = entity.DateCreated,
            CreatedByUserId = entity.CreatedByUserId,
            ChangeDescription = entity.ChangeDescription
        };
    }
}

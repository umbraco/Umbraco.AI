using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.Versioning;

/// <summary>
/// EF Core implementation of <see cref="IAiEntityVersionRepository"/>.
/// </summary>
internal sealed class EfCoreAiEntityVersionRepository : IAiEntityVersionRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreAiEntityVersionRepository"/> class.
    /// </summary>
    /// <param name="scopeProvider">The EF Core scope provider.</param>
    public EfCoreAiEntityVersionRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIEntityVersion>> GetVersionHistoryAsync(
        Guid entityId,
        string entityType,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var entities = await scope.ExecuteWithContextAsync(async db =>
        {
            return await db.EntityVersions
                .Where(v => v.EntityId == entityId && v.EntityType == entityType)
                .OrderByDescending(v => v.Version)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        });

        scope.Complete();
        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<int> GetVersionCountByEntityAsync(
        Guid entityId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var count = await scope.ExecuteWithContextAsync(async db =>
            await db.EntityVersions
                .CountAsync(v => v.EntityId == entityId && v.EntityType == entityType, cancellationToken));

        scope.Complete();
        return count;
    }

    /// <inheritdoc />
    public async Task<AIEntityVersion?> GetVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

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
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = new AIEntityVersionEntity
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
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

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
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

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
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            // Use ROW_NUMBER window function to identify excess versions per entity
            // This is more efficient than GroupBy + SelectMany and works in both SQL Server and SQLite
            var sql = @"
                DELETE FROM umbracoAiEntityVersion
                WHERE Id IN (
                    SELECT Id FROM (
                        SELECT Id,
                               ROW_NUMBER() OVER (
                                   PARTITION BY EntityId, EntityType
                                   ORDER BY Version DESC
                               ) AS RowNum
                        FROM umbracoAiEntityVersion
                    ) AS Ranked
                    WHERE RowNum > {0}
                )";

            return await db.Database.ExecuteSqlRawAsync(sql, [maxVersionsPerEntity], cancellationToken);
        });

        scope.Complete();
        return deleted;
    }

    /// <inheritdoc />
    public async Task<int> GetVersionCountAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var count = await scope.ExecuteWithContextAsync(async db =>
            await db.EntityVersions.CountAsync(cancellationToken));

        scope.Complete();
        return count;
    }

    private static AIEntityVersion MapToDomain(AIEntityVersionEntity entity)
    {
        return new AIEntityVersion
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

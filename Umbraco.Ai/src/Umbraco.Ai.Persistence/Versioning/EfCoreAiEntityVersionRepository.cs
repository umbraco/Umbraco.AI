using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Versioning;

namespace Umbraco.Ai.Persistence.Versioning;

/// <summary>
/// EF Core implementation of <see cref="IAiEntityVersionRepository"/>.
/// </summary>
internal sealed class EfCoreAiEntityVersionRepository : IAiEntityVersionRepository
{
    private readonly IDbContextFactory<UmbracoAiDbContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreAiEntityVersionRepository"/> class.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    public EfCoreAiEntityVersionRepository(IDbContextFactory<UmbracoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AiEntityVersion>> GetVersionHistoryAsync(
        Guid entityId,
        string entityType,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = db.EntityVersions
            .Where(v => v.EntityId == entityId && v.EntityType == entityType)
            .OrderByDescending(v => v.Version);

        var limitedQuery = limit.HasValue ? query.Take(limit.Value) : query;

        var entities = await limitedQuery.ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    /// <inheritdoc />
    public async Task<AiEntityVersion?> GetVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.EntityVersions
            .FirstOrDefaultAsync(
                v => v.EntityId == entityId && v.EntityType == entityType && v.Version == version,
                cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task SaveVersionAsync(
        Guid entityId,
        string entityType,
        int version,
        string snapshot,
        int? userId,
        string? changeDescription,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

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
    }

    /// <inheritdoc />
    public async Task DeleteVersionsAsync(
        Guid entityId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        await db.EntityVersions
            .Where(v => v.EntityId == entityId && v.EntityType == entityType)
            .ExecuteDeleteAsync(cancellationToken);
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

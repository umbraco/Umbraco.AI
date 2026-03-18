using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.SemanticSearch;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.SemanticSearch;

/// <summary>
/// EF Core implementation of the embeddings repository.
/// </summary>
internal class EfCoreAIEmbeddingsRepository : IAIEmbeddingsRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    public EfCoreAIEmbeddingsRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AIEmbedding?> GetByEntityKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIEmbeddingsEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Embeddings.FirstOrDefaultAsync(e => e.EntityKey == entityKey, cancellationToken));

        scope.Complete();
        return entity is null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AIEmbedding>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIEmbeddingsEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Embeddings.ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(MapToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AIEmbedding>> GetByFilterAsync(
        string? entityType = null,
        string[]? entityTypeAliases = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIEmbeddingsEntity> entities = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIEmbeddingsEntity> query = db.Embeddings;

            if (entityType is not null)
            {
                query = query.Where(e => e.EntityType == entityType);
            }

            if (entityTypeAliases is { Length: > 0 })
            {
                query = query.Where(e => entityTypeAliases.Contains(e.EntityTypeAlias));
            }

            return await query.ToListAsync(cancellationToken);
        });

        scope.Complete();
        return entities.Select(MapToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AIEmbedding>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIEmbeddingsEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.Embeddings.Where(e => e.ProfileId == profileId).ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(MapToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task SaveAsync(AIEmbedding embedding, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            AIEmbeddingsEntity? existing = await db.Embeddings
                .FirstOrDefaultAsync(e => e.EntityKey == embedding.EntityKey, cancellationToken);

            if (existing is null)
            {
                db.Embeddings.Add(MapToEntity(embedding));
            }
            else
            {
                UpdateEntity(existing, embedding);
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task SaveBatchAsync(IEnumerable<AIEmbedding> embeddings, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            foreach (var embedding in embeddings)
            {
                AIEmbeddingsEntity? existing = await db.Embeddings
                    .FirstOrDefaultAsync(e => e.EntityKey == embedding.EntityKey, cancellationToken);

                if (existing is null)
                {
                    db.Embeddings.Add(MapToEntity(embedding));
                }
                else
                {
                    UpdateEntity(existing, embedding);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task DeleteByEntityKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            AIEmbeddingsEntity? entity = await db.Embeddings
                .FirstOrDefaultAsync(e => e.EntityKey == entityKey, cancellationToken);

            if (entity is not null)
            {
                db.Embeddings.Remove(entity);
                await db.SaveChangesAsync(cancellationToken);
            }

            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task DeleteByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            var entities = await db.Embeddings
                .Where(e => e.ProfileId == profileId)
                .ToListAsync(cancellationToken);

            db.Embeddings.RemoveRange(entities);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var count = await scope.ExecuteWithContextAsync(async db =>
            await db.Embeddings.CountAsync(cancellationToken));

        scope.Complete();
        return count;
    }

    private static AIEmbedding MapToDomain(AIEmbeddingsEntity entity) => new()
    {
        Id = entity.Id,
        EntityKey = entity.EntityKey,
        EntityType = entity.EntityType,
        EntityTypeAlias = entity.EntityTypeAlias,
        Name = entity.Name,
        TextContent = entity.TextContent,
        Vector = entity.Vector,
        Dimensions = entity.Dimensions,
        ProfileId = entity.ProfileId,
        ModelId = entity.ModelId,
        DateIndexed = entity.DateIndexed,
        EntityDateModified = entity.EntityDateModified
    };

    private static AIEmbeddingsEntity MapToEntity(AIEmbedding domain) => new()
    {
        Id = domain.Id,
        EntityKey = domain.EntityKey,
        EntityType = domain.EntityType,
        EntityTypeAlias = domain.EntityTypeAlias,
        Name = domain.Name,
        TextContent = domain.TextContent,
        Vector = domain.Vector,
        Dimensions = domain.Dimensions,
        ProfileId = domain.ProfileId,
        ModelId = domain.ModelId,
        DateIndexed = domain.DateIndexed,
        EntityDateModified = domain.EntityDateModified
    };

    private static void UpdateEntity(AIEmbeddingsEntity entity, AIEmbedding domain)
    {
        entity.Id = domain.Id;
        entity.EntityType = domain.EntityType;
        entity.EntityTypeAlias = domain.EntityTypeAlias;
        entity.Name = domain.Name;
        entity.TextContent = domain.TextContent;
        entity.Vector = domain.Vector;
        entity.Dimensions = domain.Dimensions;
        entity.ProfileId = domain.ProfileId;
        entity.ModelId = domain.ModelId;
        entity.DateIndexed = domain.DateIndexed;
        entity.EntityDateModified = domain.EntityDateModified;
    }
}

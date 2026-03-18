using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.SemanticSearch;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.SemanticSearch;

/// <summary>
/// EF Core implementation of the content embedding repository.
/// </summary>
internal class EfCoreAIContentEmbeddingRepository : IAIContentEmbeddingRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    public EfCoreAIContentEmbeddingRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<ContentEmbedding?> GetByContentKeyAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIContentEmbeddingEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.ContentEmbeddings.FirstOrDefaultAsync(e => e.ContentKey == contentKey, cancellationToken));

        scope.Complete();
        return entity is null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentEmbedding>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIContentEmbeddingEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.ContentEmbeddings.ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(MapToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentEmbedding>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIContentEmbeddingEntity> entities = await scope.ExecuteWithContextAsync(async db =>
            await db.ContentEmbeddings.Where(e => e.ProfileId == profileId).ToListAsync(cancellationToken));

        scope.Complete();
        return entities.Select(MapToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task SaveAsync(ContentEmbedding embedding, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            AIContentEmbeddingEntity? existing = await db.ContentEmbeddings
                .FirstOrDefaultAsync(e => e.ContentKey == embedding.ContentKey, cancellationToken);

            if (existing is null)
            {
                db.ContentEmbeddings.Add(MapToEntity(embedding));
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
    public async Task SaveBatchAsync(IEnumerable<ContentEmbedding> embeddings, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            foreach (var embedding in embeddings)
            {
                AIContentEmbeddingEntity? existing = await db.ContentEmbeddings
                    .FirstOrDefaultAsync(e => e.ContentKey == embedding.ContentKey, cancellationToken);

                if (existing is null)
                {
                    db.ContentEmbeddings.Add(MapToEntity(embedding));
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
    public async Task DeleteByContentKeyAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            AIContentEmbeddingEntity? entity = await db.ContentEmbeddings
                .FirstOrDefaultAsync(e => e.ContentKey == contentKey, cancellationToken);

            if (entity is not null)
            {
                db.ContentEmbeddings.Remove(entity);
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
            var entities = await db.ContentEmbeddings
                .Where(e => e.ProfileId == profileId)
                .ToListAsync(cancellationToken);

            db.ContentEmbeddings.RemoveRange(entities);
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
            await db.ContentEmbeddings.CountAsync(cancellationToken));

        scope.Complete();
        return count;
    }

    private static ContentEmbedding MapToDomain(AIContentEmbeddingEntity entity) => new()
    {
        Id = entity.Id,
        ContentKey = entity.ContentKey,
        ContentType = entity.ContentType,
        ContentTypeAlias = entity.ContentTypeAlias,
        Name = entity.Name,
        TextContent = entity.TextContent,
        Vector = entity.Vector,
        Dimensions = entity.Dimensions,
        ProfileId = entity.ProfileId,
        ModelId = entity.ModelId,
        DateIndexed = entity.DateIndexed,
        ContentDateModified = entity.ContentDateModified
    };

    private static AIContentEmbeddingEntity MapToEntity(ContentEmbedding domain) => new()
    {
        Id = domain.Id,
        ContentKey = domain.ContentKey,
        ContentType = domain.ContentType,
        ContentTypeAlias = domain.ContentTypeAlias,
        Name = domain.Name,
        TextContent = domain.TextContent,
        Vector = domain.Vector,
        Dimensions = domain.Dimensions,
        ProfileId = domain.ProfileId,
        ModelId = domain.ModelId,
        DateIndexed = domain.DateIndexed,
        ContentDateModified = domain.ContentDateModified
    };

    private static void UpdateEntity(AIContentEmbeddingEntity entity, ContentEmbedding domain)
    {
        entity.Id = domain.Id;
        entity.ContentType = domain.ContentType;
        entity.ContentTypeAlias = domain.ContentTypeAlias;
        entity.Name = domain.Name;
        entity.TextContent = domain.TextContent;
        entity.Vector = domain.Vector;
        entity.Dimensions = domain.Dimensions;
        entity.ProfileId = domain.ProfileId;
        entity.ModelId = domain.ModelId;
        entity.DateIndexed = domain.DateIndexed;
        entity.ContentDateModified = domain.ContentDateModified;
    }
}

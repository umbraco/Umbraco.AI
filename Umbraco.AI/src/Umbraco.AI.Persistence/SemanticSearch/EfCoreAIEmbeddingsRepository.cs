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
    public async Task<AIEmbedding?> GetByContentKeyAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AIEmbeddingsEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Embeddings.FirstOrDefaultAsync(e => e.ContentKey == contentKey, cancellationToken));

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
        string? contentType = null,
        string[]? contentTypeAliases = null,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        List<AIEmbeddingsEntity> entities = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIEmbeddingsEntity> query = db.Embeddings;

            if (contentType is not null)
            {
                query = query.Where(e => e.ContentType == contentType);
            }

            if (contentTypeAliases is { Length: > 0 })
            {
                query = query.Where(e => contentTypeAliases.Contains(e.ContentTypeAlias));
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
                .FirstOrDefaultAsync(e => e.ContentKey == embedding.ContentKey, cancellationToken);

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
                    .FirstOrDefaultAsync(e => e.ContentKey == embedding.ContentKey, cancellationToken);

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
    public async Task DeleteByContentKeyAsync(Guid contentKey, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync(async db =>
        {
            AIEmbeddingsEntity? entity = await db.Embeddings
                .FirstOrDefaultAsync(e => e.ContentKey == contentKey, cancellationToken);

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

    private static AIEmbeddingsEntity MapToEntity(AIEmbedding domain) => new()
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

    private static void UpdateEntity(AIEmbeddingsEntity entity, AIEmbedding domain)
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

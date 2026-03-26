using System.Numerics.Tensors;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Search.Db.VectorStore;

/// <summary>
/// EF Core implementation of <see cref="IAIVectorStore"/> with brute-force cosine similarity search.
/// </summary>
/// <remarks>
/// Vectors are stored as JSON arrays in string columns. Similarity search loads candidate vectors
/// and computes cosine similarity in .NET using <see cref="TensorPrimitives"/>. Provider-specific
/// subclasses (e.g. SQL Server) can override <see cref="SearchAsync"/> to use native vector operations.
/// </remarks>
internal class EFCoreAIVectorStore : IAIVectorStore
{
    private readonly IEFCoreScopeProvider<UmbracoAISearchDbContext> _scopeProvider;

    public EFCoreAIVectorStore(IEFCoreScopeProvider<UmbracoAISearchDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>
    /// Gets the scope provider for database access. Available to subclasses for native search implementations.
    /// </summary>
    protected IEFCoreScopeProvider<UmbracoAISearchDbContext> ScopeProvider => _scopeProvider;

    /// <inheritdoc />
    public async Task UpsertAsync(string indexName, string documentId, string? culture, int chunkIndex, ReadOnlyMemory<float> vector, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<bool>(async db =>
        {
            AIVectorEntryEntity? existing = await db.VectorEntries
                .FirstOrDefaultAsync(e => e.IndexName == indexName && e.DocumentId == documentId && e.Culture == culture && e.ChunkIndex == chunkIndex, cancellationToken);

            string vectorJson = VectorToJson(vector);
            string? metadataJson = metadata is not null ? JsonSerializer.Serialize(metadata) : null;

            if (existing is null)
            {
                db.VectorEntries.Add(new AIVectorEntryEntity
                {
                    IndexName = indexName,
                    DocumentId = documentId,
                    Culture = culture,
                    ChunkIndex = chunkIndex,
                    Vector = vectorJson,
                    Metadata = metadataJson,
                });
            }
            else
            {
                existing.Vector = vectorJson;
                existing.Metadata = metadataJson;
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string indexName, string documentId, string? culture, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<bool>(async db =>
        {
            List<AIVectorEntryEntity> entities = await db.VectorEntries
                .Where(e => e.IndexName == indexName && e.DocumentId == documentId && e.Culture == culture)
                .ToListAsync(cancellationToken);

            if (entities.Count > 0)
            {
                db.VectorEntries.RemoveRange(entities);
                await db.SaveChangesAsync(cancellationToken);
            }

            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task DeleteDocumentAsync(string indexName, string documentId, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<bool>(async db =>
        {
            List<AIVectorEntryEntity> entities = await db.VectorEntries
                .Where(e => e.IndexName == indexName && e.DocumentId == documentId)
                .ToListAsync(cancellationToken);

            if (entities.Count > 0)
            {
                db.VectorEntries.RemoveRange(entities);
                await db.SaveChangesAsync(cancellationToken);
            }

            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<AIVectorSearchResult>> SearchAsync(string indexName, ReadOnlyMemory<float> queryVector, string? culture = null, int topK = 10, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        IReadOnlyList<AIVectorSearchResult> results = await scope.ExecuteWithContextAsync<IReadOnlyList<AIVectorSearchResult>>(async db =>
        {
            IQueryable<AIVectorEntryEntity> query = db.VectorEntries
                .Where(e => e.IndexName == indexName);

            // Culture filtering follows CMS Search conventions:
            // - culture provided: include that culture + invariant (null) entries
            // - culture null: include only invariant (null) entries
            if (culture is not null)
            {
                query = query.Where(e => e.Culture == culture || e.Culture == null);
            }
            else
            {
                query = query.Where(e => e.Culture == null);
            }

            List<AIVectorEntryEntity> entries = await query.ToListAsync(cancellationToken);

            if (entries.Count == 0)
            {
                return Array.Empty<AIVectorSearchResult>();
            }

            return entries
                .Select(e => new AIVectorSearchResult(
                    e.DocumentId,
                    TensorPrimitives.CosineSimilarity(queryVector.Span, JsonToVector(e.Vector)),
                    DeserializeMetadata(e.Metadata)))
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .ToList();
        });

        scope.Complete();
        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AIVectorEntry>> GetVectorsByDocumentAsync(string indexName, string documentId, string? culture = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        IReadOnlyList<AIVectorEntry> results = await scope.ExecuteWithContextAsync<IReadOnlyList<AIVectorEntry>>(async db =>
        {
            IQueryable<AIVectorEntryEntity> query = db.VectorEntries
                .Where(e => e.IndexName == indexName && e.DocumentId == documentId);

            if (culture is not null)
            {
                query = query.Where(e => e.Culture == culture);
            }

            List<AIVectorEntryEntity> entries = await query
                .OrderBy(e => e.ChunkIndex)
                .ToListAsync(cancellationToken);

            return entries
                .Select(e => new AIVectorEntry(e.DocumentId, e.Culture, e.ChunkIndex, JsonToVector(e.Vector), DeserializeMetadata(e.Metadata)))
                .ToList();
        });

        scope.Complete();
        return results;
    }

    /// <inheritdoc />
    public async Task ResetAsync(string indexName, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<bool>(async db =>
        {
            List<AIVectorEntryEntity> entries = await db.VectorEntries
                .Where(e => e.IndexName == indexName)
                .ToListAsync(cancellationToken);

            if (entries.Count > 0)
            {
                db.VectorEntries.RemoveRange(entries);
                await db.SaveChangesAsync(cancellationToken);
            }

            return true;
        });

        scope.Complete();
    }

    /// <inheritdoc />
    public async Task<long> GetDocumentCountAsync(string indexName, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        long count = await scope.ExecuteWithContextAsync(async db =>
            await db.VectorEntries.LongCountAsync(e => e.IndexName == indexName, cancellationToken));

        scope.Complete();
        return count;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    internal static string VectorToJson(ReadOnlyMemory<float> vector)
        => JsonSerializer.Serialize(vector.ToArray());

    internal static float[] JsonToVector(string json)
        => JsonSerializer.Deserialize<float[]>(json) ?? [];

    internal static IDictionary<string, object>? DeserializeMetadata(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    }
}

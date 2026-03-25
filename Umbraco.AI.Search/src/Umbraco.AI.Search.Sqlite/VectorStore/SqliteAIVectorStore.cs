using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Search.Sqlite.VectorStore;

/// <summary>
/// SQLite implementation of the vector store using brute-force in-memory similarity search.
/// </summary>
/// <remarks>
/// Vectors are stored as binary blobs (IEEE 754 float arrays). Similarity search loads
/// all candidate vectors and computes cosine similarity in .NET. A future version will
/// use sqlite-vec for native vector search.
/// </remarks>
internal sealed class SqliteAIVectorStore : IAIVectorStore
{
    private readonly IEFCoreScopeProvider<UmbracoAISearchDbContext> _scopeProvider;

    public SqliteAIVectorStore(IEFCoreScopeProvider<UmbracoAISearchDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(string indexName, string documentId, string? culture, int chunkIndex, ReadOnlyMemory<float> vector, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<bool>(async db =>
        {
            AIVectorEntryEntity? existing = await db.VectorEntries
                .FirstOrDefaultAsync(e => e.IndexName == indexName && e.DocumentId == documentId && e.Culture == culture && e.ChunkIndex == chunkIndex, cancellationToken);

            byte[] vectorBytes = VectorToBytes(vector);
            string? metadataJson = metadata is not null ? JsonSerializer.Serialize(metadata) : null;

            if (existing is null)
            {
                db.VectorEntries.Add(new AIVectorEntryEntity
                {
                    IndexName = indexName,
                    DocumentId = documentId,
                    Culture = culture,
                    ChunkIndex = chunkIndex,
                    Vector = vectorBytes,
                    Metadata = metadataJson,
                });
            }
            else
            {
                existing.Vector = vectorBytes;
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
    public async Task<IReadOnlyList<AIVectorSearchResult>> SearchAsync(string indexName, ReadOnlyMemory<float> queryVector, string? culture = null, int topK = 10, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        IReadOnlyList<AIVectorSearchResult> results = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AIVectorEntryEntity> query = db.VectorEntries
                .Where(e => e.IndexName == indexName);

            if (culture is not null)
            {
                query = query.Where(e => e.Culture == culture);
            }

            List<AIVectorEntryEntity> entries = await query.ToListAsync(cancellationToken);

            if (entries.Count == 0)
            {
                return (IReadOnlyList<AIVectorSearchResult>)Array.Empty<AIVectorSearchResult>();
            }

            return (IReadOnlyList<AIVectorSearchResult>)entries
                .Select(e => new AIVectorSearchResult(
                    e.DocumentId,
                    TensorPrimitives.CosineSimilarity(queryVector.Span, BytesToVector(e.Vector)),
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

        IReadOnlyList<AIVectorEntry> results = await scope.ExecuteWithContextAsync(async db =>
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

            return (IReadOnlyList<AIVectorEntry>)entries
                .Select(e => new AIVectorEntry(e.DocumentId, e.Culture, e.ChunkIndex, BytesToVector(e.Vector).ToArray(), DeserializeMetadata(e.Metadata)))
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

    private static byte[] VectorToBytes(ReadOnlyMemory<float> vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        MemoryMarshal.AsBytes(vector.Span).CopyTo(bytes);
        return bytes;
    }

    private static ReadOnlySpan<float> BytesToVector(byte[] bytes)
        => MemoryMarshal.Cast<byte, float>(bytes);

    private static IDictionary<string, object>? DeserializeMetadata(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    }
}

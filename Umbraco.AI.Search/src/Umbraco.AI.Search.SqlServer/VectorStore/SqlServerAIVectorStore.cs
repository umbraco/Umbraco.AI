using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Search.SqlServer.VectorStore;

/// <summary>
/// SQL Server implementation of the vector store.
/// </summary>
/// <remarks>
/// On SQL Server 2025+, uses <c>VECTOR_DISTANCE()</c> with <c>CAST</c> for server-side
/// cosine similarity search. On older versions, falls back to loading vectors and computing
/// cosine similarity in .NET (same approach as the SQLite provider).
/// </remarks>
internal sealed class SqlServerAIVectorStore : IAIVectorStore
{
    private readonly IEFCoreScopeProvider<UmbracoAISearchDbContext> _scopeProvider;
    private bool? _supportsNativeVectors;

    public SqlServerAIVectorStore(IEFCoreScopeProvider<UmbracoAISearchDbContext> scopeProvider)
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
            if (await SupportsNativeVectorsAsync(db, cancellationToken))
            {
                return await SearchNativeAsync(db, indexName, queryVector, culture, topK, cancellationToken);
            }

            return await SearchBruteForceAsync(db, indexName, queryVector, culture, topK, cancellationToken);
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
                .Select(e => new AIVectorEntry(e.DocumentId, e.Culture, e.ChunkIndex, VectorBytesToFloats(e.Vector), DeserializeMetadata(e.Metadata)))
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

    // ── Search strategies ───────────────────────────────────────────────

    /// <summary>
    /// SQL Server 2025+ path: uses <c>VECTOR_DISTANCE('cosine', CAST(... AS vector(1536)), ...)</c>
    /// for server-side similarity ranking.
    /// </summary>
    private static async Task<IReadOnlyList<AIVectorSearchResult>> SearchNativeAsync(
        UmbracoAISearchDbContext db,
        string indexName,
        ReadOnlyMemory<float> queryVector,
        string? culture,
        int topK,
        CancellationToken cancellationToken)
    {
        byte[] queryBytes = VectorToBytes(queryVector);

        // VECTOR_DISTANCE returns distance (lower = more similar), so 1 - distance = similarity.
        // CAST from varbinary to vector at query time — avoids requiring vector column type in schema.
        List<VectorSearchRow> rows = culture is not null
            ? await db.Database
                .SqlQuery<VectorSearchRow>(
                    $"""
                    SELECT DocumentId,
                           1.0 - VECTOR_DISTANCE('cosine', CAST(Vector AS vector(1536)), CAST({queryBytes} AS vector(1536))) AS Score,
                           Metadata
                    FROM umbracoAISearchVectorEntry
                    WHERE IndexName = {indexName} AND (Culture = {culture} OR Culture IS NULL)
                    ORDER BY Score DESC
                    OFFSET 0 ROWS FETCH NEXT {topK} ROWS ONLY
                    """)
                .ToListAsync(cancellationToken)
            : await db.Database
                .SqlQuery<VectorSearchRow>(
                    $"""
                    SELECT DocumentId,
                           1.0 - VECTOR_DISTANCE('cosine', CAST(Vector AS vector(1536)), CAST({queryBytes} AS vector(1536))) AS Score,
                           Metadata
                    FROM umbracoAISearchVectorEntry
                    WHERE IndexName = {indexName} AND Culture IS NULL
                    ORDER BY Score DESC
                    OFFSET 0 ROWS FETCH NEXT {topK} ROWS ONLY
                    """)
                .ToListAsync(cancellationToken);

        return rows
            .Select(r => new AIVectorSearchResult(r.DocumentId, r.Score, DeserializeMetadata(r.Metadata)))
            .ToList();
    }

    /// <summary>
    /// Pre-2025 fallback: loads candidate vectors and computes cosine similarity in .NET.
    /// </summary>
    private static async Task<IReadOnlyList<AIVectorSearchResult>> SearchBruteForceAsync(
        UmbracoAISearchDbContext db,
        string indexName,
        ReadOnlyMemory<float> queryVector,
        string? culture,
        int topK,
        CancellationToken cancellationToken)
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
                TensorPrimitives.CosineSimilarity(queryVector.Span, BytesToVector(e.Vector)),
                DeserializeMetadata(e.Metadata)))
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    // ── Version detection ───────────────────────────────────────────────

    /// <summary>
    /// Detects whether the SQL Server instance supports native vector operations (version 17+).
    /// Result is cached for the lifetime of this instance.
    /// </summary>
    private async Task<bool> SupportsNativeVectorsAsync(UmbracoAISearchDbContext db, CancellationToken cancellationToken)
    {
        if (_supportsNativeVectors.HasValue)
        {
            return _supportsNativeVectors.Value;
        }

        try
        {
            var majorVersion = await db.Database
                .SqlQuery<int>($"SELECT CAST(SERVERPROPERTY('ProductMajorVersion') AS int) AS [Value]")
                .SingleAsync(cancellationToken);

            _supportsNativeVectors = majorVersion >= 17;
        }
        catch
        {
            // If we can't determine the version, assume no native vector support
            _supportsNativeVectors = false;
        }

        return _supportsNativeVectors.Value;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static byte[] VectorToBytes(ReadOnlyMemory<float> vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        MemoryMarshal.AsBytes(vector.Span).CopyTo(bytes);
        return bytes;
    }

    private static float[] VectorBytesToFloats(byte[] bytes)
        => MemoryMarshal.Cast<byte, float>(bytes).ToArray();

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

    /// <summary>
    /// Row type for raw SQL vector search results.
    /// </summary>
    private sealed class VectorSearchRow
    {
        public string DocumentId { get; set; } = string.Empty;
        public double Score { get; set; }
        public string? Metadata { get; set; }
    }
}

using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Search.SqlServer.VectorStore;

/// <summary>
/// SQL Server implementation of the vector store using native <c>vector</c> type
/// and <c>VECTOR_DISTANCE()</c> for server-side similarity search.
/// </summary>
internal sealed class SqlServerAIVectorStore : IAIVectorStore
{
    private readonly IEFCoreScopeProvider<UmbracoAISearchDbContext> _scopeProvider;

    public SqlServerAIVectorStore(IEFCoreScopeProvider<UmbracoAISearchDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(string indexName, string documentId, int chunkIndex, ReadOnlyMemory<float> vector, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        await scope.ExecuteWithContextAsync<bool>(async db =>
        {
            AIVectorEntryEntity? existing = await db.VectorEntries
                .FirstOrDefaultAsync(e => e.IndexName == indexName && e.DocumentId == documentId && e.ChunkIndex == chunkIndex, cancellationToken);

            byte[] vectorBytes = VectorToBytes(vector);
            string? metadataJson = metadata is not null ? JsonSerializer.Serialize(metadata) : null;

            if (existing is null)
            {
                db.VectorEntries.Add(new AIVectorEntryEntity
                {
                    IndexName = indexName,
                    DocumentId = documentId,
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
    public async Task DeleteAsync(string indexName, string documentId, CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<AIVectorSearchResult>> SearchAsync(string indexName, ReadOnlyMemory<float> queryVector, int topK = 10, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAISearchDbContext> scope = _scopeProvider.CreateScope();

        byte[] queryBytes = VectorToBytes(queryVector);

        IReadOnlyList<AIVectorSearchResult> results = await scope.ExecuteWithContextAsync(async db =>
        {
            // Use VECTOR_DISTANCE for server-side cosine similarity search.
            // VECTOR_DISTANCE returns distance (lower = more similar), so we compute 1 - distance for similarity score.
            List<VectorSearchRow> rows = await db.Database
                .SqlQuery<VectorSearchRow>(
                    $"""
                    SELECT DocumentId, 1.0 - VECTOR_DISTANCE('cosine', Vector, {queryBytes}) AS Score, Metadata
                    FROM umbracoAISearchVectorEntry
                    WHERE IndexName = {indexName}
                    ORDER BY Score DESC
                    OFFSET 0 ROWS FETCH NEXT {topK} ROWS ONLY
                    """)
                .ToListAsync(cancellationToken);

            return (IReadOnlyList<AIVectorSearchResult>)rows
                .Select(r => new AIVectorSearchResult(r.DocumentId, r.Score, DeserializeMetadata(r.Metadata)))
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

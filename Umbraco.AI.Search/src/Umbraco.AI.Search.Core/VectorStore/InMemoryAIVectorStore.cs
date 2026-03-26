using System.Collections.Concurrent;
using System.Numerics.Tensors;

namespace Umbraco.AI.Search.Core.VectorStore;

/// <summary>
/// In-memory vector store for development and testing. Not suitable for production use.
/// </summary>
public sealed class InMemoryAIVectorStore : IAIVectorStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<ChunkKey, VectorEntry>> _indexes = new();

    public Task UpsertAsync(string indexName, string documentId, string? culture, int chunkIndex, ReadOnlyMemory<float> vector, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        ConcurrentDictionary<ChunkKey, VectorEntry> index = _indexes.GetOrAdd(indexName, _ => new ConcurrentDictionary<ChunkKey, VectorEntry>());
        var key = new ChunkKey(documentId, culture, chunkIndex);
        index[key] = new VectorEntry(vector.ToArray(), metadata);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string indexName, string documentId, string? culture, CancellationToken cancellationToken = default)
    {
        if (_indexes.TryGetValue(indexName, out ConcurrentDictionary<ChunkKey, VectorEntry>? index))
        {
            var keysToRemove = index.Keys
                .Where(k => k.DocumentId == documentId && k.Culture == culture)
                .ToList();

            foreach (var key in keysToRemove)
            {
                index.TryRemove(key, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteDocumentAsync(string indexName, string documentId, CancellationToken cancellationToken = default)
    {
        if (_indexes.TryGetValue(indexName, out ConcurrentDictionary<ChunkKey, VectorEntry>? index))
        {
            var keysToRemove = index.Keys
                .Where(k => k.DocumentId == documentId)
                .ToList();

            foreach (var key in keysToRemove)
            {
                index.TryRemove(key, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AIVectorSearchResult>> SearchAsync(string indexName, ReadOnlyMemory<float> queryVector, string? culture = null, int topK = 10, CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out ConcurrentDictionary<ChunkKey, VectorEntry>? index) || index.IsEmpty)
        {
            return Task.FromResult<IReadOnlyList<AIVectorSearchResult>>(Array.Empty<AIVectorSearchResult>());
        }

        // Culture filtering follows CMS Search conventions:
        // - culture provided: include that culture + invariant (null) entries
        // - culture null: include only invariant (null) entries
        var entries = culture is not null
            ? index.Where(kvp => kvp.Key.Culture == culture || kvp.Key.Culture is null)
            : index.Where(kvp => kvp.Key.Culture is null);

        IReadOnlyList<AIVectorSearchResult> results = entries
            .Select(kvp => new AIVectorSearchResult(
                kvp.Key.DocumentId,
                TensorPrimitives.CosineSimilarity(queryVector.Span, kvp.Value.Vector.AsSpan()),
                kvp.Value.Metadata))
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult(results);
    }

    public Task<IReadOnlyList<AIVectorEntry>> GetVectorsByDocumentAsync(string indexName, string documentId, string? culture = null, CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out ConcurrentDictionary<ChunkKey, VectorEntry>? index))
        {
            return Task.FromResult<IReadOnlyList<AIVectorEntry>>(Array.Empty<AIVectorEntry>());
        }

        IReadOnlyList<AIVectorEntry> results = index
            .Where(kvp => kvp.Key.DocumentId == documentId && (culture is null || kvp.Key.Culture == culture))
            .OrderBy(kvp => kvp.Key.ChunkIndex)
            .Select(kvp => new AIVectorEntry(kvp.Key.DocumentId, kvp.Key.Culture, kvp.Key.ChunkIndex, kvp.Value.Vector, kvp.Value.Metadata))
            .ToList();

        return Task.FromResult(results);
    }

    public Task ResetAsync(string indexName, CancellationToken cancellationToken = default)
    {
        _indexes.TryRemove(indexName, out _);
        return Task.CompletedTask;
    }

    public Task<long> GetDocumentCountAsync(string indexName, CancellationToken cancellationToken = default)
    {
        if (_indexes.TryGetValue(indexName, out ConcurrentDictionary<ChunkKey, VectorEntry>? index))
        {
            return Task.FromResult((long)index.Count);
        }

        return Task.FromResult(0L);
    }

    private sealed record ChunkKey(string DocumentId, string? Culture, int ChunkIndex);

    private sealed record VectorEntry(float[] Vector, IDictionary<string, object>? Metadata);
}

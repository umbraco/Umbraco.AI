using System.Collections.Concurrent;
using System.Numerics.Tensors;

namespace Umbraco.AI.Search.Core.VectorStore;

/// <summary>
/// In-memory vector store for development and testing. Not suitable for production use.
/// </summary>
public sealed class InMemoryAIVectorStore : IAIVectorStore
{
    // indexName -> (documentId, chunkIndex) -> entry
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<ChunkKey, VectorEntry>> _indexes = new();

    public Task UpsertAsync(string indexName, string documentId, int chunkIndex, ReadOnlyMemory<float> vector, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        ConcurrentDictionary<ChunkKey, VectorEntry> index = _indexes.GetOrAdd(indexName, _ => new ConcurrentDictionary<ChunkKey, VectorEntry>());
        var key = new ChunkKey(documentId, chunkIndex);
        index[key] = new VectorEntry(vector.ToArray(), metadata);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string indexName, string documentId, CancellationToken cancellationToken = default)
    {
        if (_indexes.TryGetValue(indexName, out ConcurrentDictionary<ChunkKey, VectorEntry>? index))
        {
            var keysToRemove = index.Keys.Where(k => k.DocumentId == documentId).ToList();
            foreach (var key in keysToRemove)
            {
                index.TryRemove(key, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AIVectorSearchResult>> SearchAsync(string indexName, ReadOnlyMemory<float> queryVector, int topK = 10, CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out ConcurrentDictionary<ChunkKey, VectorEntry>? index) || index.IsEmpty)
        {
            return Task.FromResult<IReadOnlyList<AIVectorSearchResult>>(Array.Empty<AIVectorSearchResult>());
        }

        IReadOnlyList<AIVectorSearchResult> results = index
            .Select(kvp => new AIVectorSearchResult(
                kvp.Key.DocumentId,
                TensorPrimitives.CosineSimilarity(queryVector.Span, kvp.Value.Vector.AsSpan()),
                kvp.Value.Metadata))
            .OrderByDescending(r => r.Score)
            .Take(topK)
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

    private sealed record ChunkKey(string DocumentId, int ChunkIndex);

    private sealed record VectorEntry(float[] Vector, IDictionary<string, object>? Metadata);
}

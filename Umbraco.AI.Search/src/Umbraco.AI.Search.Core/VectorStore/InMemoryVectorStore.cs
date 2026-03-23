using System.Collections.Concurrent;
using System.Numerics.Tensors;

namespace Umbraco.AI.Search.Core.VectorStore;

/// <summary>
/// In-memory vector store for development and testing. Not suitable for production use.
/// </summary>
public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, VectorEntry>> _indexes = new();

    public Task UpsertAsync(string indexName, string documentId, ReadOnlyMemory<float> vector, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        ConcurrentDictionary<string, VectorEntry> index = _indexes.GetOrAdd(indexName, _ => new ConcurrentDictionary<string, VectorEntry>());
        index[documentId] = new VectorEntry(vector.ToArray(), metadata);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string indexName, string documentId, CancellationToken cancellationToken = default)
    {
        if (_indexes.TryGetValue(indexName, out ConcurrentDictionary<string, VectorEntry>? index))
        {
            index.TryRemove(documentId, out _);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string indexName, ReadOnlyMemory<float> queryVector, int topK = 10, CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out ConcurrentDictionary<string, VectorEntry>? index) || index.IsEmpty)
        {
            return Task.FromResult<IReadOnlyList<VectorSearchResult>>(Array.Empty<VectorSearchResult>());
        }

        IReadOnlyList<VectorSearchResult> results = index
            .Select(kvp => new VectorSearchResult(
                kvp.Key,
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
        if (_indexes.TryGetValue(indexName, out ConcurrentDictionary<string, VectorEntry>? index))
        {
            return Task.FromResult((long)index.Count);
        }

        return Task.FromResult(0L);
    }

    private sealed record VectorEntry(float[] Vector, IDictionary<string, object>? Metadata);
}

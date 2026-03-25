namespace Umbraco.AI.Search.Core.VectorStore;

/// <summary>
/// Abstraction for vector storage backends used by the AI search provider.
/// </summary>
public interface IAIVectorStore
{
    /// <summary>
    /// Inserts or updates a vector chunk with associated metadata.
    /// </summary>
    Task UpsertAsync(string indexName, string documentId, int chunkIndex, ReadOnlyMemory<float> vector, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all vector chunks for a document from the store.
    /// </summary>
    Task DeleteAsync(string indexName, string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a similarity search against stored vectors.
    /// </summary>
    Task<IReadOnlyList<AIVectorSearchResult>> SearchAsync(string indexName, ReadOnlyMemory<float> queryVector, int topK = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all vectors for a given index.
    /// </summary>
    Task ResetAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of documents stored in the index.
    /// </summary>
    Task<long> GetDocumentCountAsync(string indexName, CancellationToken cancellationToken = default);
}

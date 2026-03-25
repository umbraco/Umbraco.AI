namespace Umbraco.AI.Search.Core.VectorStore;

/// <summary>
/// Abstraction for vector storage backends used by the AI search provider.
/// </summary>
public interface IAIVectorStore
{
    /// <summary>
    /// Inserts or updates a vector chunk with associated metadata.
    /// </summary>
    Task UpsertAsync(string indexName, string documentId, string? culture, int chunkIndex, ReadOnlyMemory<float> vector, IDictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all vector chunks for a document and culture from the store.
    /// Pass <c>null</c> for culture to target invariant content only.
    /// </summary>
    Task DeleteAsync(string indexName, string documentId, string? culture, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all vector chunks for a document across all cultures.
    /// </summary>
    Task DeleteDocumentAsync(string indexName, string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a similarity search against stored vectors, optionally filtered by culture.
    /// </summary>
    Task<IReadOnlyList<AIVectorSearchResult>> SearchAsync(string indexName, ReadOnlyMemory<float> queryVector, string? culture = null, int topK = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all vectors for a given index.
    /// </summary>
    Task ResetAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of vector entries stored in the index.
    /// </summary>
    Task<long> GetDocumentCountAsync(string indexName, CancellationToken cancellationToken = default);
}

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Repository for persisting and querying content embeddings.
/// </summary>
internal interface IAIContentEmbeddingRepository
{
    /// <summary>
    /// Gets an embedding by content key.
    /// </summary>
    Task<ContentEmbedding?> GetByContentKeyAsync(Guid contentKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all stored embeddings.
    /// </summary>
    Task<IReadOnlyList<ContentEmbedding>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets embeddings matching the specified filters.
    /// </summary>
    /// <param name="contentType">Filter by content type (e.g., "content", "media"), or null for all.</param>
    /// <param name="contentTypeAliases">Filter by content type aliases, or null/empty for all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ContentEmbedding>> GetByFilterAsync(
        string? contentType = null,
        string[]? contentTypeAliases = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all embeddings for a specific profile.
    /// </summary>
    Task<IReadOnlyList<ContentEmbedding>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (upserts) an embedding by content key.
    /// </summary>
    Task SaveAsync(ContentEmbedding embedding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a batch of embeddings.
    /// </summary>
    Task SaveBatchAsync(IEnumerable<ContentEmbedding> embeddings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an embedding by content key.
    /// </summary>
    Task DeleteByContentKeyAsync(Guid contentKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all embeddings for a specific profile.
    /// </summary>
    Task DeleteByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of stored embeddings.
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}

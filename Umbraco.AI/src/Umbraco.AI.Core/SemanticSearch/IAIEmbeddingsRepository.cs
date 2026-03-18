namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Repository for persisting and querying embeddings.
/// </summary>
internal interface IAIEmbeddingsRepository
{
    /// <summary>
    /// Gets an embedding by entity key.
    /// </summary>
    Task<AIEmbedding?> GetByEntityKeyAsync(Guid entityKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all stored embeddings.
    /// </summary>
    Task<IReadOnlyList<AIEmbedding>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets embeddings matching the specified filters.
    /// </summary>
    /// <param name="entityType">Filter by entity type (e.g., "content", "media"), or null for all.</param>
    /// <param name="entityTypeAliases">Filter by entity type aliases, or null/empty for all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<AIEmbedding>> GetByFilterAsync(
        string? entityType = null,
        string[]? entityTypeAliases = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all embeddings for a specific profile.
    /// </summary>
    Task<IReadOnlyList<AIEmbedding>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (upserts) an embedding by entity key.
    /// </summary>
    Task SaveAsync(AIEmbedding embedding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a batch of embeddings.
    /// </summary>
    Task SaveBatchAsync(IEnumerable<AIEmbedding> embeddings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an embedding by entity key.
    /// </summary>
    Task DeleteByEntityKeyAsync(Guid entityKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all embeddings for a specific profile.
    /// </summary>
    Task DeleteByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of stored embeddings.
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}

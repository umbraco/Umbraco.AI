namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Provides semantic search capabilities using vector embeddings.
/// </summary>
internal interface IAISemanticSearchService
{
    /// <summary>
    /// Searches for entities semantically similar to the given query.
    /// </summary>
    /// <param name="query">The natural language search query.</param>
    /// <param name="options">Optional search options for filtering and limiting results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of semantic search results ordered by similarity.</returns>
    Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(
        string query,
        SemanticSearchQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes or re-indexes a specific entity by its key and type.
    /// </summary>
    /// <param name="entityKey">The entity key.</param>
    /// <param name="entityType">The entity type identifier (e.g., "content", "media").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IndexEntityAsync(Guid entityKey, string entityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity's embedding from the index.
    /// </summary>
    /// <param name="entityKey">The entity key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveEntityAsync(Guid entityKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reindexes all entities from all registered semantic index sources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReindexAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of the semantic search index.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index status information.</returns>
    Task<SemanticSearchIndexStatus> GetIndexStatusAsync(CancellationToken cancellationToken = default);
}

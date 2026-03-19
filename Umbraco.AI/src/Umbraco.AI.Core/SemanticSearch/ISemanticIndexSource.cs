namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Defines a pluggable source for semantic indexing. Each implementation handles
/// a specific entity type (e.g., content, media, members) and knows how to
/// extract text and enumerate entities for that type.
/// </summary>
internal interface ISemanticIndexSource
{
    /// <summary>
    /// Gets the unique entity type identifier (e.g., "content", "media").
    /// </summary>
    string EntityType { get; }

    /// <summary>
    /// Gets the extracted entry for a specific entity by its key.
    /// </summary>
    /// <param name="entityKey">The unique entity key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The semantic index entry, or null if the entity was not found or has no embeddable text.</returns>
    Task<SemanticIndexEntry?> GetEntryAsync(Guid entityKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates all entities of this type for reindexing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of semantic index entries.</returns>
    IAsyncEnumerable<SemanticIndexEntry> GetAllEntriesAsync(CancellationToken cancellationToken = default);
}

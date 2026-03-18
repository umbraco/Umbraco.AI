namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Represents a single semantic search result.
/// </summary>
/// <param name="EntityKey">The Umbraco entity key.</param>
/// <param name="Name">The entity name.</param>
/// <param name="EntityType">The entity type (e.g., "content", "media").</param>
/// <param name="EntityTypeAlias">The entity type alias.</param>
/// <param name="SimilarityScore">The cosine similarity score (0.0 to 1.0).</param>
/// <param name="TextSnippet">A snippet of the indexed text content.</param>
internal record SemanticSearchResult(
    Guid EntityKey,
    string Name,
    string EntityType,
    string EntityTypeAlias,
    float SimilarityScore,
    string? TextSnippet);

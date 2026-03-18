namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Represents a single semantic search result.
/// </summary>
/// <param name="ContentKey">The Umbraco content/media key.</param>
/// <param name="Name">The content name.</param>
/// <param name="ContentType">The type: "content" or "media".</param>
/// <param name="ContentTypeAlias">The content type alias.</param>
/// <param name="SimilarityScore">The cosine similarity score (0.0 to 1.0).</param>
/// <param name="TextSnippet">A snippet of the indexed text content.</param>
internal record SemanticSearchResult(
    Guid ContentKey,
    string Name,
    string ContentType,
    string ContentTypeAlias,
    float SimilarityScore,
    string? TextSnippet);

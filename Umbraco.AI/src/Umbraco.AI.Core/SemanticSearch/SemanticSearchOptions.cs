namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Options for a semantic search query.
/// </summary>
/// <param name="TypeFilter">Filter by "content", "media", or null for all.</param>
/// <param name="EntitySubTypes">Filter by entity sub-types.</param>
/// <param name="MaxResults">Maximum number of results to return.</param>
/// <param name="MinimumSimilarity">Minimum cosine similarity threshold.</param>
internal record SemanticSearchQueryOptions(
    string? TypeFilter = null,
    string[]? EntitySubTypes = null,
    int MaxResults = 10,
    float MinimumSimilarity = 0.5f);

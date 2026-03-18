namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Options for a semantic search query.
/// </summary>
/// <param name="TypeFilter">Filter by "content", "media", or null for all.</param>
/// <param name="EntityTypeAliases">Filter by entity type aliases.</param>
/// <param name="MaxResults">Maximum number of results to return.</param>
/// <param name="MinimumSimilarity">Minimum cosine similarity threshold.</param>
internal record SemanticSearchQueryOptions(
    string? TypeFilter = null,
    string[]? EntityTypeAliases = null,
    int MaxResults = 10,
    float MinimumSimilarity = 0.5f);

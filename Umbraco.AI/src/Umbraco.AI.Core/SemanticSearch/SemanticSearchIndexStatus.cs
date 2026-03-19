namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Status information about the semantic search index.
/// </summary>
/// <param name="TotalIndexed">Number of indexed documents.</param>
/// <param name="ProfileId">The embedding profile ID used for indexing.</param>
/// <param name="ModelId">The model identifier used for indexing.</param>
public record SemanticSearchIndexStatus(
    int TotalIndexed,
    Guid? ProfileId,
    string? ModelId);

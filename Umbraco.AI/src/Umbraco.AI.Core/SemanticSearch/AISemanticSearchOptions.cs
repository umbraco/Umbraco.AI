namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Configuration options for semantic search, bound to "Umbraco:AI:SemanticSearch".
/// </summary>
public class AISemanticSearchOptions
{
    /// <summary>
    /// Gets or sets whether semantic search is enabled. Default is true (opt-out).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum text length to embed per document. Default is 8000 characters.
    /// </summary>
    public int MaxTextLength { get; set; } = 8000;

    /// <summary>
    /// Gets or sets the batch size for reindex operations. Default is 50.
    /// </summary>
    public int BatchSize { get; set; } = 50;
}

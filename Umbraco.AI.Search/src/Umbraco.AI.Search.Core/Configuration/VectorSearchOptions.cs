namespace Umbraco.AI.Search.Core.Configuration;

/// <summary>
/// Configuration options for AI vector search.
/// </summary>
public sealed class VectorSearchOptions
{
    /// <summary>
    /// The embedding profile alias to use for generating vectors.
    /// When null, the default embedding profile is used.
    /// </summary>
    public string? EmbeddingProfileAlias { get; set; }

    /// <summary>
    /// Maximum number of results to return from vector similarity search
    /// before applying further filtering.
    /// </summary>
    public int DefaultTopK { get; set; } = 100;
}

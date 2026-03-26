namespace Umbraco.AI.Search.Core.Configuration;

/// <summary>
/// Configuration options for AI vector search.
/// </summary>
public sealed class AIVectorSearchOptions
{
    /// <summary>
    /// Maximum number of results to return from vector similarity search
    /// before applying further filtering.
    /// </summary>
    public int DefaultTopK { get; set; } = 100;

    /// <summary>
    /// Minimum cosine similarity score (0.0–1.0) for a result to be included.
    /// Results below this threshold are discarded as irrelevant.
    /// </summary>
    public double MinScore { get; set; } = 0.3;

    /// <summary>
    /// Maximum number of tokens per chunk when splitting content for embedding.
    /// </summary>
    public int ChunkSize { get; set; } = 512;

    /// <summary>
    /// Number of tokens of overlap between consecutive chunks.
    /// Set to 0 to disable overlap.
    /// </summary>
    public int ChunkOverlap { get; set; } = 50;
}

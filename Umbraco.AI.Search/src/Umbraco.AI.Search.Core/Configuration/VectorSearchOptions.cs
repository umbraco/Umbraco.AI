namespace Umbraco.AI.Search.Core.Configuration;

/// <summary>
/// Configuration options for AI vector search.
/// </summary>
public sealed class VectorSearchOptions
{
    /// <summary>
    /// Maximum number of results to return from vector similarity search
    /// before applying further filtering.
    /// </summary>
    public int DefaultTopK { get; set; } = 100;
}

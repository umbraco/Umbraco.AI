namespace Umbraco.AI.Search.Core.Chunking;

/// <summary>
/// Options controlling how text is split into chunks.
/// </summary>
public class AITextChunkingOptions
{
    /// <summary>
    /// Maximum number of tokens per chunk.
    /// </summary>
    public int MaxChunkSize { get; set; } = 512;

    /// <summary>
    /// Number of tokens of overlap between consecutive chunks.
    /// Set to 0 to disable overlap.
    /// </summary>
    public int ChunkOverlap { get; set; } = 50;
}

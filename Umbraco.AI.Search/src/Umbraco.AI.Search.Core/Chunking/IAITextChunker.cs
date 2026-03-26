namespace Umbraco.AI.Search.Core.Chunking;

/// <summary>
/// Splits text into chunks suitable for embedding generation.
/// </summary>
public interface IAITextChunker
{
    /// <summary>
    /// Splits the given text into chunks according to the specified options.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <param name="options">Chunking configuration.</param>
    /// <returns>An ordered list of text chunks.</returns>
    IReadOnlyList<AITextChunk> ChunkText(string text, AITextChunkingOptions options);
}

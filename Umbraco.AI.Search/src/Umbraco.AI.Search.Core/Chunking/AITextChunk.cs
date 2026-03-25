namespace Umbraco.AI.Search.Core.Chunking;

/// <summary>
/// Represents a chunk of text produced by an <see cref="IAITextChunker"/>.
/// </summary>
/// <param name="Text">The chunk text content.</param>
/// <param name="Index">Zero-based position of this chunk within the source document.</param>
/// <param name="StartOffset">Character offset where this chunk begins in the original text.</param>
/// <param name="Length">Character length of this chunk in the original text (excluding overlap).</param>
public record AITextChunk(string Text, int Index, int StartOffset, int Length);

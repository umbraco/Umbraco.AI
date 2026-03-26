namespace Umbraco.AI.Search.Core.VectorStore;

/// <summary>
/// Represents a stored vector entry for a document chunk.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="Culture">The culture, or null for invariant content.</param>
/// <param name="ChunkIndex">The chunk index within the document.</param>
/// <param name="Vector">The stored embedding vector.</param>
/// <param name="Metadata">Optional metadata stored alongside the vector.</param>
public record AIVectorEntry(string DocumentId, string? Culture, int ChunkIndex, ReadOnlyMemory<float> Vector, IDictionary<string, object>? Metadata);

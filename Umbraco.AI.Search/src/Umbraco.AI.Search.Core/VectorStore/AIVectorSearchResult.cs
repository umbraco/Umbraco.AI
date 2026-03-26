namespace Umbraco.AI.Search.Core.VectorStore;

/// <summary>
/// Represents a single result from a vector similarity search.
/// </summary>
/// <param name="DocumentId">The identifier of the matched document.</param>
/// <param name="Score">The similarity score (higher is more similar).</param>
/// <param name="Metadata">Optional metadata stored alongside the vector.</param>
public record AIVectorSearchResult(string DocumentId, double Score, IDictionary<string, object>? Metadata);

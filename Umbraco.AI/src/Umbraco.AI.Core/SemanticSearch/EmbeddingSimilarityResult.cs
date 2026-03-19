namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Represents an embedding paired with its similarity score from a vector search.
/// </summary>
/// <param name="Embedding">The matched embedding.</param>
/// <param name="SimilarityScore">The similarity score (0.0 to 1.0).</param>
internal record EmbeddingSimilarityResult(AIEmbedding Embedding, float SimilarityScore);

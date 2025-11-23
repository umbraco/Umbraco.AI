namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Request model for generating embeddings.
/// </summary>
public class AiEmbeddingRequest : AiRequestBase
{
    /// <summary>
    /// The input texts to generate embeddings for.
    /// </summary>
    public required IReadOnlyList<string> Inputs { get; init; }
}
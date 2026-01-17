using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Embeddings;

namespace Umbraco.Ai.Core.Chat.Middleware;

/// <summary>
/// Chat middleware that tracks AI chat usage.
/// </summary>
public sealed class AiTrackingEmbeddingMiddleware : IAiEmbeddingMiddleware
{
    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
        => new AiTrackingEmbeddingGenerator<string, Embedding<float>>(generator);
}

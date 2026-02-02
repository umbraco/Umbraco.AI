using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Embeddings;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Chat middleware that tracks AI chat usage.
/// </summary>
public sealed class AITrackingEmbeddingMiddleware : IAiEmbeddingMiddleware
{
    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
        => new AITrackingEmbeddingGenerator<string, Embedding<float>>(generator);
}

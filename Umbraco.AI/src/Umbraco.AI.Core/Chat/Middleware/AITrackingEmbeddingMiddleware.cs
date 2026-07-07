using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Embedding middleware that records usage analytics and audit entries for embedding
/// generations, via the shared <see cref="IAIOperationTracker"/>.
/// </summary>
internal sealed class AITrackingEmbeddingMiddleware : IAIEmbeddingMiddleware
{
    private readonly IAIOperationTracker _tracker;
    private readonly IAIRuntimeContextAccessor _contextAccessor;

    public AITrackingEmbeddingMiddleware(IAIOperationTracker tracker, IAIRuntimeContextAccessor contextAccessor)
    {
        _tracker = tracker;
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
        => new AITrackingEmbeddingGenerator(generator, _tracker, _contextAccessor);
}

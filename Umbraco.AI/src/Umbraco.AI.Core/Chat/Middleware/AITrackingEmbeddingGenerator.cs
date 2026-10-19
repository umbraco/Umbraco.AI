using Microsoft.Extensions.AI;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Embedding generator that records usage analytics and audit entries around an embedding
/// generation, by delegating to the shared <see cref="IAIOperationTracker"/>. Replaces the former
/// separate tracking/usage-recording/auditing embedding generator trio with a single
/// tracker-backed generator.
/// </summary>
internal sealed class AITrackingEmbeddingGenerator : AIBoundEmbeddingGeneratorBase<string, Embedding<float>>
{
    private readonly IAIOperationTracker _tracker;

    public AITrackingEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
        IAIOperationTracker tracker)
        : base(innerGenerator)
        => _tracker = tracker;

    /// <inheritdoc />
    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var valueList = values.ToList();
        var descriptor = new AIOperationDescriptor
        {
            Capability = AICapability.Embedding,
            PromptData = valueList,
        };

        var tracked = await _tracker.TrackAsync(
            descriptor,
            async token =>
            {
                var result = await base.GenerateAsync(valueList, options, token);
                return new AITrackedOperationResult<GeneratedEmbeddings<Embedding<float>>>
                {
                    Result = result,
                    Usage = result.Usage,
                    ResponseData = result,
                };
            },
            cancellationToken);

        return tracked.Result;
    }
}

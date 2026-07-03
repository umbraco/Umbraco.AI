using Microsoft.Extensions.AI;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;

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

    public AITrackingEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator, IAIOperationTracker tracker)
        : base(innerGenerator)
    {
        _tracker = tracker;
    }

    /// <inheritdoc />
    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var valueList = values.ToList();
        var descriptor = new AIOperationDescriptor
        {
            Capability = AICapability.Embedding,
            PromptData = valueList,
            Metadata = ExtractMetadataFromOptions(options),
            RecordUsageWhenEmpty = false,
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
                    AuditResponse = new AIAuditResponse { Data = result, Usage = result.Usage },
                };
            },
            cancellationToken);

        return tracked.Result;
    }

    /// <summary>
    /// Extracts audit metadata (LogKeys) from the embedding options' additional properties.
    /// Unlike chat/speech-to-text, embedding has no runtime-context-derived metadata source, so
    /// this reproduces the extraction the former <c>AIAuditingEmbeddingGenerator</c> performed
    /// directly against <see cref="EmbeddingGenerationOptions.AdditionalProperties"/>.
    /// </summary>
    private static IReadOnlyDictionary<string, string>? ExtractMetadataFromOptions(EmbeddingGenerationOptions? options)
    {
        if (options?.AdditionalProperties?.TryGetValue(Constants.ContextKeys.LogKeys, out var logKeys) == true
            && logKeys is IEnumerable<string> keys)
        {
            return keys.ToDictionary(
                key => key,
                key => options?.AdditionalProperties?[key]?.ToString() ?? string.Empty);
        }

        return null;
    }
}

using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Chat.Middleware;

internal sealed class AITrackingEmbeddingGenerator<TInput, TEmbedding>(
    IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
    : AIBoundEmbeddingGeneratorBase<TInput, TEmbedding>(innerGenerator)
    where TEmbedding : Embedding
{
    /// <summary>
    /// The last usage details received from the generator.
    /// </summary>
    public UsageDetails? LastUsageDetails { get; private set; }
    
    /// <summary>
    /// The last value received from the generator.
    /// </summary>
    public IEnumerable<TEmbedding>? LastEmbeddings { get; private set; }

    /// <inheritdoc />
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await base.GenerateAsync(values, options, cancellationToken);

        LastUsageDetails = result.Usage;
        LastEmbeddings = result;
        
        return result;
    }
}
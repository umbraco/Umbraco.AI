using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Chat.Middleware;

internal sealed class AiTrackingEmbeddingGenerator<TInput, TEmbedding>(
    IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
    : AiBoundEmbeddingGeneratorBase<TInput, TEmbedding>(innerGenerator)
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
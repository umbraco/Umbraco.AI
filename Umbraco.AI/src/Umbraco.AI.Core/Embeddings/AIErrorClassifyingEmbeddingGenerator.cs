using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Core.Embeddings;

/// <summary>
/// An embedding generator decorator that translates provider SDK exceptions into a classified
/// <see cref="AIProviderException"/> using the originating provider's
/// <see cref="IAIProvider.ClassifyError"/>.
/// </summary>
/// <remarks>
/// Applied innermost by <see cref="AIEmbeddingGeneratorFactory"/> — around the provider's generator
/// and beneath the middleware pipeline. Cancellation propagates untouched; an already-classified
/// <see cref="AIProviderException"/> passes through unchanged.
/// </remarks>
internal sealed class AIErrorClassifyingEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IAIProvider _provider;

    public AIErrorClassifyingEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
        IAIProvider provider)
        : base(innerGenerator)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GenerateAsync(values, options, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (AIProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AIProviderException(_provider.ClassifyError(ex), ex);
        }
    }
}

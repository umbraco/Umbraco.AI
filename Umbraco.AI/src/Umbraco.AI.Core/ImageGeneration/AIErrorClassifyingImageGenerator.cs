using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Providers.Errors;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Internal plumbing for the experimental image-generation API

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// An image-generator decorator that translates provider SDK exceptions into a classified
/// <see cref="AIProviderException"/> using the originating provider's
/// <see cref="IAIProvider.ClassifyError"/>.
/// </summary>
/// <remarks>
/// Applied innermost by <see cref="AIImageGeneratorFactory"/> — around the provider's generator
/// and beneath the middleware pipeline. Cancellation propagates untouched; an already-classified
/// <see cref="AIProviderException"/> passes through unchanged.
/// </remarks>
internal sealed class AIErrorClassifyingImageGenerator : AIBoundImageGeneratorBase
{
    private readonly IAIProvider _provider;

    public AIErrorClassifyingImageGenerator(IImageGenerator innerGenerator, IAIProvider provider)
        : base(innerGenerator)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    public override async Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GenerateAsync(request, options, cancellationToken);
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
            throw Classify(ex);
        }
    }

    private AIProviderException Classify(Exception ex)
        => new(_provider.ClassifyError(ex), ex);
}

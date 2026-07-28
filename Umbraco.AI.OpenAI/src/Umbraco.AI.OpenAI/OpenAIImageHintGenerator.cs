using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Image generator decorator that turns the provider-specific <c>quality</c> and <c>style</c> hints into the
/// OpenAI SDK options the request is actually built from.
/// </summary>
/// <remarks>
/// Wrapped outside the M.E.AI adapter but inside everything else, so the hints are translated no matter which
/// caller assembled the <see cref="ImageGenerationOptions"/> — the profile's own settings, or a direct
/// <see cref="IImageGenerator"/> consumer. Without it the hints are silently dropped; see
/// <see cref="OpenAIImageHints"/>.
/// </remarks>
internal sealed class OpenAIImageHintGenerator(IImageGenerator innerGenerator, ILogger? logger)
    : DelegatingImageGenerator(innerGenerator)
{
    /// <inheritdoc />
    public override Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GenerateAsync(request, OpenAIImageHints.Apply(options, logger), cancellationToken);
}

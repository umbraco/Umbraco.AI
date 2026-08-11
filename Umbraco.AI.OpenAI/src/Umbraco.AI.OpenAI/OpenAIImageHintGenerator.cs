using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Image generator decorator that turns the provider-specific <c>quality</c> and <c>style</c> hints into the
/// OpenAI SDK options the request is actually built from.
/// </summary>
/// <remarks>
/// Covers a direct <see cref="IImageGenerator"/> consumer that passes hints as additional properties, which
/// the adapter ignores. A profile's own settings arrive typed through the capability's
/// <c>ApplyCapabilitySettings</c> hook and are already a raw representation by the time they reach here, so
/// this leaves them alone. See <see cref="OpenAIImageHints"/>.
/// </remarks>
internal sealed class OpenAIImageHintGenerator(
    IImageGenerator innerGenerator,
    string? boundModelId,
    ILogger? logger)
    : DelegatingImageGenerator(innerGenerator)
{
    /// <inheritdoc />
    public override Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GenerateAsync(request, OpenAIImageHints.Apply(options, boundModelId, logger), cancellationToken);
}

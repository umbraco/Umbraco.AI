using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.ImageGeneration;
using Umbraco.AI.Core.Models;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Image generator decorator that removes the core request options the capability declares the target model
/// does not accept, before delegating to the inner generator.
/// </summary>
/// <remarks>
/// The image counterpart of <see cref="DeclaredSettingsChatClient"/>. Size is usually described by
/// enumerating what a model accepts (<c>image.supportedSizes</c>) rather than declaring it unsupported, but
/// media type is a real case: <c>output_format</c> is a gpt-image parameter that the DALL·E models have no
/// equivalent for.
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
internal sealed class DeclaredSettingsImageGenerator(
    IImageGenerator innerGenerator,
    IAICapability capability,
    string? boundModelId,
    ILogger? logger)
    : DelegatingImageGenerator(innerGenerator)
{
    /// <inheritdoc />
    public override Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GenerateAsync(request, Filter(options), cancellationToken);

    private ImageGenerationOptions? Filter(ImageGenerationOptions? options)
    {
        if (options is null || (options.ImageSize is null && string.IsNullOrWhiteSpace(options.MediaType)))
        {
            return options;
        }

        var modelId = options.ModelId ?? boundModelId;
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return options;
        }

        var unsupported = capability.GetSettingsSupport(modelId).AsProfileSettingKeys();
        ImageGenerationOptions? filtered = null;

        if (options.ImageSize is not null && unsupported.Contains(AIProfileSettingKeys.Size))
        {
            (filtered ??= options.Clone()).ImageSize = null;
        }

        if (!string.IsNullOrWhiteSpace(options.MediaType) && unsupported.Contains(AIProfileSettingKeys.MediaType))
        {
            (filtered ??= options.Clone()).MediaType = null;
        }

        if (filtered is null)
        {
            return options;
        }

        logger?.LogDebug(
            "Model '{ModelId}' declares {Unsupported} unsupported; removed from the request.",
            modelId,
            string.Join(", ", unsupported));

        return filtered;
    }
}

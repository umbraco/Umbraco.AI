using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Embedding generator decorator that removes the core request options the capability declares the target
/// model does not accept, before delegating to the inner generator.
/// </summary>
/// <remarks>
/// The embedding counterpart of <see cref="DeclaredSettingsChatClient"/>, installed by the embedding
/// capability bases. In practice this is <c>Dimensions</c>, which only the newer embedding models accept.
/// </remarks>
internal sealed class DeclaredSettingsEmbeddingGenerator(
    IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
    IAICapability capability,
    string? boundModelId,
    ILogger? logger)
    : DelegatingEmbeddingGenerator<string, Embedding<float>>(innerGenerator)
{
    /// <inheritdoc />
    public override Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GenerateAsync(values, Filter(options), cancellationToken);

    private EmbeddingGenerationOptions? Filter(EmbeddingGenerationOptions? options)
    {
        if (options?.Dimensions is null)
        {
            return options;
        }

        var modelId = options.ModelId ?? boundModelId;
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return options;
        }

        var declaration = capability.GetSettingsSupport(modelId);
        if (!declaration.AsProfileSettingKeys().Contains(AIProfileSettingKeys.Dimensions))
        {
            return options;
        }

        logger?.LogDebug(
            "Model '{ModelId}' declares dimensions unsupported; removed from the request.",
            modelId);

        // Clone so the caller's instance is never mutated.
        var filtered = options.Clone();
        filtered.Dimensions = null;
        return filtered;
    }
}

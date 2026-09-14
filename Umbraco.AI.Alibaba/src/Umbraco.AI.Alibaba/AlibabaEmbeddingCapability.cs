using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Alibaba;

/// <summary>
/// AI embedding capability for the Alibaba Cloud (Qwen) provider.
/// </summary>
public class AlibabaEmbeddingCapability(AlibabaProvider provider) : AIEmbeddingCapabilityBase<AlibabaProviderSettings>(provider)
{
    private const string DefaultEmbeddingModel = "text-embedding-v4";

    private new AlibabaProvider Provider => (AlibabaProvider)base.Provider;

    /// <summary>
    /// Patterns that match Alibaba text embedding models. A plain "embedding" substring
    /// match is used rather than a "^text-embedding-" prefix because not all of Alibaba's
    /// embedding models follow that naming — e.g. "qwen3.7-text-embedding" is prefixed
    /// with the model family instead. Confirmed against a live model list during development.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"embedding", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        AlibabaProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsEmbeddingModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                AlibabaModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(AlibabaProviderSettings settings, string? modelId)
        => AlibabaProvider.CreateAlibabaClient(settings)
            .GetEmbeddingClient(modelId ?? DefaultEmbeddingModel)
            .AsIEmbeddingGenerator();

    private static bool IsEmbeddingModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}

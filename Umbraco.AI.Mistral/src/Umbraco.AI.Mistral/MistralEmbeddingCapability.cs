using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Mistral;

/// <summary>
/// AI embedding capability for Mistral provider.
/// </summary>
public class MistralEmbeddingCapability(MistralProvider provider) : AIEmbeddingCapabilityBase<MistralProviderSettings>(provider)
{
    private const string DefaultEmbeddingModel = "mistral-embed";

    private new MistralProvider Provider => (MistralProvider)base.Provider;

    /// <summary>
    /// Patterns that match Mistral embedding models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"embed", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        MistralProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsEmbeddingModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                MistralModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(MistralProviderSettings settings, string? modelId)
    {
        var effectiveModelId = modelId ?? DefaultEmbeddingModel;
        var embeddings = MistralProvider.CreateMistralClient(settings).Embeddings;

        return new EmbeddingGeneratorBuilder<string, Embedding<float>>(embeddings)
            .ConfigureOptions(options => options.ModelId ??= effectiveModelId)
            .Build();
    }

    private static bool IsEmbeddingModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}

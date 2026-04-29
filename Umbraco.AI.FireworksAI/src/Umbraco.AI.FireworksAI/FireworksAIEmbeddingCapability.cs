using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.FireworksAI;

/// <summary>
/// AI embedding capability for Fireworks AI.
/// </summary>
public class FireworksAIEmbeddingCapability(FireworksAIProvider provider)
    : AIEmbeddingCapabilityBase<FireworksAIProviderSettings>(provider)
{
    private const string DefaultEmbeddingModel = "accounts/fireworks/models/qwen3-embedding-8b";

    private new FireworksAIProvider Provider => (FireworksAIProvider)base.Provider;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        FireworksAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelsAsync(settings, cancellationToken);

        return allModels
            .Where(IsEmbeddingModel)
            .Select(m => new AIModelDescriptor(
                new AIModelRef(Provider.Id, m.Name),
                FireworksAIModelUtilities.FormatDisplayName(m.Name)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(
        FireworksAIProviderSettings settings, string? modelId)
        => FireworksAIProvider.CreateOpenAIClient(settings)
            .GetEmbeddingClient(modelId ?? DefaultEmbeddingModel)
            .AsIEmbeddingGenerator();

    private static bool IsEmbeddingModel(FireworksAIModelInfo model)
        => string.Equals(model.Kind, "EMBEDDING_MODEL", StringComparison.OrdinalIgnoreCase);
}

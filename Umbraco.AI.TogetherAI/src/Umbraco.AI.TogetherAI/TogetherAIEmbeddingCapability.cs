using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.TogetherAI;

/// <summary>
/// AI embedding capability for Together AI provider.
/// Filters models dynamically by Together's declared <c>type</c> field, so no
/// code changes are required when Together adds new embedding models.
/// </summary>
public class TogetherAIEmbeddingCapability(TogetherAIProvider provider)
    : AIEmbeddingCapabilityBase<TogetherAIProviderSettings>(provider)
{
    private const string DefaultEmbeddingModel = "BAAI/bge-large-en-v1.5";
    private const string EmbeddingType = "embedding";

    private new TogetherAIProvider Provider => (TogetherAIProvider)base.Provider;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        TogetherAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelsAsync(settings, cancellationToken);

        return allModels
            .Where(m => string.Equals(m.Type, EmbeddingType, StringComparison.OrdinalIgnoreCase))
            .Select(m => new AIModelDescriptor(
                new AIModelRef(Provider.Id, m.Id),
                m.DisplayName ?? TogetherAIModelUtilities.FormatDisplayName(m.Id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(
        TogetherAIProviderSettings settings,
        string? modelId)
        => TogetherAIProvider.CreateOpenAIClient(settings)
            .GetEmbeddingClient(modelId ?? DefaultEmbeddingModel)
            .AsIEmbeddingGenerator();
}

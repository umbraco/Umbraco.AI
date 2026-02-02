using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// AI embedding capability for Microsoft AI Foundry provider.
/// </summary>
/// <remarks>
/// Supports all embedding models available through Microsoft AI Foundry, including
/// OpenAI (text-embedding-3-small, text-embedding-3-large) and other models.
/// </remarks>
public class MicrosoftFoundryEmbeddingCapability(MicrosoftFoundryProvider provider) : AIEmbeddingCapabilityBase<MicrosoftFoundryProviderSettings>(provider)
{
    private const string DefaultEmbeddingModel = "text-embedding-3-small";

    private new MicrosoftFoundryProvider Provider => (MicrosoftFoundryProvider)base.Provider;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        MicrosoftFoundryProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelsAsync(settings, cancellationToken);

        return allModels
            .Where(IsEmbeddingModel)
            .Select(m => new AIModelDescriptor(
                new AIModelRef(Provider.Id, m.Id),
                MicrosoftFoundryModelUtilities.FormatDisplayName(m.Id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(MicrosoftFoundryProviderSettings settings, string? modelId)
    {
        var model = modelId ?? DefaultEmbeddingModel;
        return MicrosoftFoundryProvider.CreateEmbeddingsClient(settings, model)
            .AsIEmbeddingGenerator(model);
    }

    private static bool IsEmbeddingModel(MicrosoftFoundryModelInfo model)
    {
        // If capabilities are provided, use them
        if (model.Capabilities is not null)
        {
            return model.Capabilities.Embeddings;
        }

        // Fallback: look for embedding model patterns
        var id = model.Id.ToLowerInvariant();
        return id.Contains("embedding") || id.Contains("embed");
    }
}

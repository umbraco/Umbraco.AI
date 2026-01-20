using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.MicrosoftFoundry;

/// <summary>
/// AI embedding capability for Microsoft AI Foundry provider.
/// </summary>
/// <remarks>
/// Supports all embedding models available through Microsoft AI Foundry, including
/// OpenAI (text-embedding-3-small, text-embedding-3-large) and other models.
/// </remarks>
public class MicrosoftFoundryEmbeddingCapability(MicrosoftFoundryProvider provider) : AiEmbeddingCapabilityBase<MicrosoftFoundryProviderSettings>(provider)
{
    private const string DefaultEmbeddingModel = "text-embedding-3-small";

    private new MicrosoftFoundryProvider Provider => (MicrosoftFoundryProvider)base.Provider;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        MicrosoftFoundryProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelsAsync(settings, cancellationToken).ConfigureAwait(false);

        return allModels
            .Where(IsEmbeddingModel)
            .Select(m => new AiModelDescriptor(
                new AiModelRef(Provider.Id, m.Id),
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

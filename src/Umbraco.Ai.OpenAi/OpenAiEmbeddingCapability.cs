using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Extensions;

namespace Umbraco.Ai.OpenAi;

/// <summary>
/// AI embedding capability for OpenAI provider.
/// </summary>
public class OpenAiEmbeddingCapability(OpenAiProvider provider) : AiEmbeddingCapabilityBase<OpenAiProviderSettings>(provider)
{
    private new OpenAiProvider Provider => (OpenAiProvider)base.Provider;

    /// <summary>
    /// Patterns that match embedding models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^text-embedding-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        OpenAiProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken).ConfigureAwait(false);

        return allModels
            .Where(IsEmbeddingModel)
            .Select(id => new AiModelDescriptor(
                new AiModelRef(Provider.Id, id),
                OpenAiModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(OpenAiProviderSettings settings)
        => OpenAiProvider.CreateOpenAiClient(settings)
            .GetEmbeddingClient("text-embedding-3-small")
            .AsIEmbeddingGenerator();

    private static bool IsEmbeddingModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}
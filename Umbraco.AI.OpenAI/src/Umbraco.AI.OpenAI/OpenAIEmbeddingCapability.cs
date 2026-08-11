using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// AI embedding capability for OpenAI provider.
/// </summary>
public class OpenAIEmbeddingCapability(OpenAIProvider provider) : AIEmbeddingCapabilityBase<OpenAIProviderSettings>(provider)
{
    private const string DefaultEmbeddingModel = "text-embedding-3-small";
    
    private new OpenAIProvider Provider => (OpenAIProvider)base.Provider;

    /// <summary>
    /// Patterns that match embedding models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^text-embedding-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        OpenAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsEmbeddingModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                OpenAIModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Shortened embeddings are a <c>text-embedding-3</c> feature, so a profile's Dimensions cannot apply to
    /// <c>ada-002</c>. Declaring it here hides the field for those models in the profile editor and, because
    /// the base enforces what is declared, also strips the value from any request that still carries one —
    /// a profile saved before a model change, or an alias-driven API caller.
    /// </remarks>
    public override AIModelSettingsSupport GetSettingsSupport(string modelId)
        => OpenAIModelUtilities.SupportsDimensions(modelId)
            ? AIModelSettingsSupport.Default
            : new AIModelSettingsSupport
            {
                UnsupportedProfileSettings = [AIProfileSettingKeys.Dimensions],
            };

    /// <inheritdoc />
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(OpenAIProviderSettings settings, string? modelId)
        => OpenAIProvider.CreateOpenAIClient(settings)
            .GetEmbeddingClient(modelId ?? DefaultEmbeddingModel)
            .AsIEmbeddingGenerator();

    private static bool IsEmbeddingModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}
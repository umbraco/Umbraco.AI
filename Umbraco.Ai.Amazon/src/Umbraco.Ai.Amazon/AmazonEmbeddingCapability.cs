using System.Text.RegularExpressions;
using Amazon.BedrockRuntime;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Extensions;

namespace Umbraco.Ai.Amazon;

/// <summary>
/// AI embedding capability for Amazon Bedrock provider.
/// </summary>
public class AmazonEmbeddingCapability(AmazonProvider provider) : AiEmbeddingCapabilityBase<AmazonProviderSettings>(provider)
{
    /// <summary>
    /// Optional region prefix pattern for inference profile IDs (e.g., "eu.", "us.", "apac.").
    /// </summary>
    private const string RegionPrefixPattern = @"(eu\.|us\.|apac\.)?";

    private new AmazonProvider Provider => (AmazonProvider)base.Provider;

    /// <summary>
    /// Patterns that match embedding models in Bedrock (with optional region prefix for inference profiles).
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new($@"^{RegionPrefixPattern}amazon\.titan-embed-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new($@"^{RegionPrefixPattern}cohere\.embed-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        AmazonProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken).ConfigureAwait(false);

        return allModels
            .Where(IsEmbeddingModel)
            .Select(id => new AiModelDescriptor(
                new AiModelRef(Provider.Id, id),
                AmazonModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(AmazonProviderSettings settings, string? modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException(
                "A model must be selected for Amazon Bedrock. " +
                "Please select a model from the available inference profiles.");
        }

        var client = AmazonProvider.CreateBedrockRuntimeClient(settings);
        var embeddingGenerator = client.AsIEmbeddingGenerator(modelId);

        // Wrap with metadata filter to remove Umbraco.Ai keys that Bedrock doesn't accept
        return new AmazonMetadataFilteringEmbeddingGenerator(embeddingGenerator);
    }

    private static bool IsEmbeddingModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}

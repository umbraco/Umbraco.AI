using System.Text.RegularExpressions;
using Amazon.BedrockRuntime;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Amazon;

/// <summary>
/// AI embedding capability for Amazon Bedrock provider.
/// </summary>
public class AmazonEmbeddingCapability(AmazonProvider provider) : AIEmbeddingCapabilityBase<AmazonProviderSettings>(provider)
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
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        AmazonProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsEmbeddingModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
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
        return client.AsIEmbeddingGenerator(modelId);
    }

    private static bool IsEmbeddingModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}

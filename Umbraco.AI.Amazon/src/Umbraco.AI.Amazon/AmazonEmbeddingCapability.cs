using System.Text.RegularExpressions;
using Amazon.Bedrock;
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

    /// <summary>
    /// Pattern matching any Cohere embed model (with or without region prefix). Cohere models require a
    /// different request/response shape than Titan and need a custom generator.
    /// </summary>
    private static readonly Regex CohereEmbedPattern =
        new($@"^{RegionPrefixPattern}cohere\.embed-", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        AmazonProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        // Embedding models reach us via two Bedrock APIs:
        //   ListFoundationModels (EMBEDDING modality) surfaces direct on-demand models like Titan and
        //   Cohere Embed v3, which have no inference profile. ListInferenceProfiles surfaces newer
        //   cross-region models like Cohere Embed v4, which is only invokable via a profile ID
        //   (e.g. eu.cohere.embed-v4:0) and does not appear in ListFoundationModels under ON_DEMAND.
        // Union both so the full set of usable embedding models is discoverable.
        var foundationModelsTask = Provider.GetAvailableFoundationModelIdsAsync(
            settings,
            ModelModality.EMBEDDING,
            cancellationToken);
        var inferenceProfilesTask = Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        await Task.WhenAll(foundationModelsTask, inferenceProfilesTask);

        return foundationModelsTask.Result
            .Concat(inferenceProfilesTask.Result.Where(IsEmbeddingModel))
            .Distinct()
            .OrderBy(id => id)
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
                "Please select a model from the available embedding models.");
        }

        var client = AmazonProvider.CreateBedrockRuntimeClient(settings);

        // AWSSDK.Extensions.Bedrock.MEAI's AsIEmbeddingGenerator hardcodes the Titan request/response
        // shape ({ "inputText": "..." } / { "embedding": [...] }). Cohere uses a different protocol
        // ({ "texts": [...], "input_type": "...", "embedding_types": [...] } and
        // { "embeddings": { "float": [[...]] } }), so we provide our own generator for Cohere.
        if (CohereEmbedPattern.IsMatch(modelId))
        {
            return new CohereEmbeddingGenerator(client, modelId);
        }

        return client.AsIEmbeddingGenerator(modelId);
    }

    private static bool IsEmbeddingModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}

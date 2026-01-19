using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Amazon;

/// <summary>
/// A delegating embedding generator that filters out Umbraco.Ai metadata keys from EmbeddingGenerationOptions
/// before sending requests to Amazon Bedrock, which doesn't accept extra keys.
/// </summary>
internal sealed class AmazonMetadataFilteringEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private const string UmbracoAiKeyPrefix = "Umbraco.Ai.";

    public AmazonMetadataFilteringEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator)
        : base(innerGenerator)
    {
    }

    public override Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var filteredOptions = FilterOptions(options);
        return base.GenerateAsync(values, filteredOptions, cancellationToken);
    }

    private static EmbeddingGenerationOptions? FilterOptions(EmbeddingGenerationOptions? options)
    {
        if (options?.AdditionalProperties is null || options.AdditionalProperties.Count == 0)
        {
            return options;
        }

        // Check if there are any Umbraco.Ai keys to filter
        var hasUmbracoKeys = options.AdditionalProperties.Keys
            .Any(k => k.StartsWith(UmbracoAiKeyPrefix, StringComparison.Ordinal));

        if (!hasUmbracoKeys)
        {
            return options;
        }

        // Clone options and filter out Umbraco.Ai keys
        var filteredOptions = options.Clone();
        var keysToRemove = filteredOptions.AdditionalProperties!.Keys
            .Where(k => k.StartsWith(UmbracoAiKeyPrefix, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            filteredOptions.AdditionalProperties.Remove(key);
        }

        return filteredOptions;
    }
}

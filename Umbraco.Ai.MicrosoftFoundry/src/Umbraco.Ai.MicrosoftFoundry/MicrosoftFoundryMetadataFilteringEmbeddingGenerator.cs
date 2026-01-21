using Microsoft.Extensions.AI;

namespace Umbraco.Ai.MicrosoftFoundry;

/// <summary>
/// An embedding generator decorator that filters out Umbraco.Ai metadata properties from requests.
/// </summary>
/// <remarks>
/// Microsoft AI Foundry rejects requests containing unknown properties in AdditionalProperties.
/// Umbraco.Ai adds metadata properties (e.g., Umbraco.Ai.ProfileId, Umbraco.Ai.ModelId) for
/// auditing and telemetry purposes. This wrapper filters them out before sending to the API.
/// </remarks>
internal sealed class MicrosoftFoundryMetadataFilteringEmbeddingGenerator(
    IEmbeddingGenerator<string, Embedding<float>> innerGenerator)
    : DelegatingEmbeddingGenerator<string, Embedding<float>>(innerGenerator)
{
    private const string UmbracoAiPrefix = "Umbraco.Ai.";

    /// <inheritdoc />
    public override Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return base.GenerateAsync(values, FilterOptions(options), cancellationToken);
    }

    private static EmbeddingGenerationOptions? FilterOptions(EmbeddingGenerationOptions? options)
    {
        if (options?.AdditionalProperties is null || options.AdditionalProperties.Count == 0)
        {
            return options;
        }

        // Check if any keys need filtering
        var hasUmbracoKeys = options.AdditionalProperties.Keys
            .Any(k => k.StartsWith(UmbracoAiPrefix, StringComparison.Ordinal));

        if (!hasUmbracoKeys)
        {
            return options;
        }

        // Clone options and filter out Umbraco.Ai.* keys
        var filteredOptions = options.Clone();
        filteredOptions.AdditionalProperties = new AdditionalPropertiesDictionary(
            options.AdditionalProperties.Where(kvp =>
                !kvp.Key.StartsWith(UmbracoAiPrefix, StringComparison.Ordinal)));

        return filteredOptions;
    }
}

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Umbraco.Ai.MicrosoftFoundry;

/// <summary>
/// A chat client decorator that filters out Umbraco.Ai metadata properties from requests.
/// </summary>
/// <remarks>
/// Microsoft AI Foundry rejects requests containing unknown properties in AdditionalProperties.
/// Umbraco.Ai adds metadata properties (e.g., Umbraco.Ai.ProfileId, Umbraco.Ai.ModelId) for
/// auditing and telemetry purposes. This wrapper filters them out before sending to the API.
/// </remarks>
internal sealed class MicrosoftFoundryMetadataFilteringChatClient(IChatClient innerClient) : IChatClient
{
    private const string UmbracoAiPrefix = "Umbraco.Ai.";

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return innerClient.GetResponseAsync(
            chatMessages,
            FilterOptions(options),
            cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in innerClient.GetStreamingResponseAsync(
            chatMessages,
            FilterOptions(options),
            cancellationToken))
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? key = null)
    {
        if (serviceType == typeof(MicrosoftFoundryMetadataFilteringChatClient))
        {
            return this;
        }

        return innerClient.GetService(serviceType, key);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        innerClient.Dispose();
    }

    private static ChatOptions? FilterOptions(ChatOptions? options)
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

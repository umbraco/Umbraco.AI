using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Amazon;

/// <summary>
/// A delegating chat client that filters out Umbraco.Ai metadata keys from ChatOptions
/// before sending requests to Amazon Bedrock, which doesn't accept extra keys.
/// </summary>
internal sealed class AmazonMetadataFilteringChatClient : DelegatingChatClient
{
    private const string UmbracoAiKeyPrefix = "Umbraco.Ai.";

    public AmazonMetadataFilteringChatClient(IChatClient innerClient)
        : base(innerClient)
    {
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var filteredOptions = FilterOptions(options);
        return base.GetResponseAsync(chatMessages, filteredOptions, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var filteredOptions = FilterOptions(options);
        return base.GetStreamingResponseAsync(chatMessages, filteredOptions, cancellationToken);
    }

    private static ChatOptions? FilterOptions(ChatOptions? options)
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

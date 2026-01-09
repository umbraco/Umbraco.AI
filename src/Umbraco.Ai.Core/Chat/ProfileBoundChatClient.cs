using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Contexts.Resolvers;

namespace Umbraco.Ai.Core.Chat;

/// <summary>
/// A chat client decorator that automatically injects the profile ID into all requests.
/// This ensures profile contexts are always resolved without callers needing to remember
/// to pass the profile ID in ChatOptions.
/// </summary>
internal sealed class ProfileBoundChatClient : IChatClient
{
    private readonly IChatClient _innerClient;
    private readonly Guid _profileId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileBoundChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="profileId">The profile ID to inject into all requests.</param>
    public ProfileBoundChatClient(IChatClient innerClient, Guid profileId)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _profileId = profileId;
    }

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return _innerClient.GetResponseAsync(chatMessages, EnsureProfileId(options), cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in _innerClient.GetStreamingResponseAsync(chatMessages, EnsureProfileId(options), cancellationToken))
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? key = null)
    {
        // Return self if requested
        if (serviceType == typeof(ProfileBoundChatClient))
        {
            return this;
        }

        // Delegate to inner client for other services
        return _innerClient.GetService(serviceType, key);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _innerClient.Dispose();
    }

    private ChatOptions EnsureProfileId(ChatOptions? options)
    {
        options ??= new ChatOptions();
        options.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        options.AdditionalProperties.TryAdd(ProfileContextResolver.ProfileIdKey, _profileId);
        return options;
    }
}

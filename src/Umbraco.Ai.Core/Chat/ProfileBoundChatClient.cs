using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Contexts.Resolvers;

namespace Umbraco.Ai.Core.Chat;

/// <summary>
/// A chat client decorator that automatically injects the profile ID into all requests.
/// This ensures profile contexts are always resolved without callers needing to remember
/// to pass the profile ID in ChatOptions.
/// </summary>
internal sealed class ProfileBoundChatClient : BoundChatClientBase
{
    private readonly Guid _profileId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileBoundChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="profileId">The profile ID to inject into all requests.</param>
    public ProfileBoundChatClient(IChatClient innerClient, Guid profileId)
        : base(innerClient)
    {
        _profileId = profileId;
    }

    /// <inheritdoc />
    protected override ChatOptions? TransformOptions(ChatOptions? options)
    {
        options ??= new ChatOptions();
        options.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        options.AdditionalProperties.TryAdd(ProfileContextResolver.ProfileIdKey, _profileId);
        return options;
    }
}

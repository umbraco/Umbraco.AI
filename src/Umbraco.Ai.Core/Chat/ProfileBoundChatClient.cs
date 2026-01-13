using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Contexts.Resolvers;
using Umbraco.Ai.Core.Audit;
using Umbraco.Ai.Core.Profiles;

namespace Umbraco.Ai.Core.Chat;

/// <summary>
/// A chat client decorator that automatically injects the profile ID and telemetry metadata into all requests.
/// This ensures profile contexts are always resolved and telemetry data is captured without callers needing
/// to remember to pass metadata in ChatOptions.
/// </summary>
internal sealed class ProfileBoundChatClient : BoundChatClientBase
{
    private readonly AiProfile _profile;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileBoundChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="profile">The profile to bind to all requests.</param>
    public ProfileBoundChatClient(IChatClient innerClient, AiProfile profile)
        : base(innerClient)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <inheritdoc />
    protected override ChatOptions? TransformOptions(ChatOptions? options)
    {
        options ??= new ChatOptions();
        options.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        
        options.AdditionalProperties.TryAdd(Constants.MetadataKeys.ProfileId, _profile.Id);
        options.AdditionalProperties.TryAdd(Constants.MetadataKeys.ProfileAlias, _profile.Alias);
        options.AdditionalProperties.TryAdd(Constants.MetadataKeys.ProviderId, _profile.Model.ProviderId);
        // Note: ModelId should come from options.ModelId, not profile, as it can be overridden

        return options;
    }
}

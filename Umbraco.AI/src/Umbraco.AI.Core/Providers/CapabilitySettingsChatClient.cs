using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Chat client decorator that applies provider-declared, profile-level settings (e.g. reasoning
/// effort) onto each request's <see cref="ChatOptions"/> before delegating to the inner client.
/// </summary>
/// <remarks>
/// Created by <see cref="AIChatCapabilityBase{TSettings, TCapabilitySettings}"/> with the resolved,
/// typed profile settings baked in. The caller's <see cref="ChatOptions"/> instance is never mutated;
/// a per-request copy is used.
/// </remarks>
/// <typeparam name="TCapabilitySettings">The provider-declared profile settings type.</typeparam>
internal sealed class CapabilitySettingsChatClient<TCapabilitySettings> : DelegatingChatClient
    where TCapabilitySettings : class
{
    private readonly TCapabilitySettings _capabilitySettings;
    private readonly Action<TCapabilitySettings, ChatOptions> _apply;

    public CapabilitySettingsChatClient(
        IChatClient innerClient,
        TCapabilitySettings capabilitySettings,
        Action<TCapabilitySettings, ChatOptions> apply)
        : base(innerClient)
    {
        _capabilitySettings = capabilitySettings;
        _apply = apply;
    }

    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(messages, Apply(options), cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(messages, Apply(options), cancellationToken);

    private ChatOptions Apply(ChatOptions? options)
    {
        // Clone so the caller's options instance is never mutated.
        var effective = options?.Clone() ?? new ChatOptions();
        _apply(_capabilitySettings, effective);
        return effective;
    }
}

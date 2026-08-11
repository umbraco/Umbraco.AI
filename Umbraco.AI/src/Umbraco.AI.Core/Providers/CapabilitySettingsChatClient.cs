using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Chat client decorator that applies provider-declared capability settings (e.g. reasoning
/// effort) onto each request's <see cref="ChatOptions"/> before delegating to the inner client.
/// </summary>
/// <remarks>
/// Created by <see cref="AIChatCapabilityBase{TSettings, TCapabilitySettings}"/> with the resolved,
/// typed capability settings baked in. The caller's <see cref="ChatOptions"/> instance is never mutated;
/// a per-request copy is used.
/// </remarks>
/// <typeparam name="TCapabilitySettings">The provider-declared capability settings type.</typeparam>
internal sealed class CapabilitySettingsChatClient<TCapabilitySettings> : DelegatingChatClient
    where TCapabilitySettings : class
{
    private readonly TCapabilitySettings _capabilitySettings;
    private readonly string? _boundModelId;
    private readonly Action<TCapabilitySettings, string?, ChatOptions> _apply;

    public CapabilitySettingsChatClient(
        IChatClient innerClient,
        TCapabilitySettings capabilitySettings,
        string? boundModelId,
        Action<TCapabilitySettings, string?, ChatOptions> apply)
        : base(innerClient)
    {
        _capabilitySettings = capabilitySettings;
        _boundModelId = boundModelId;
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

        // Resolve the model the request will actually run against so the provider can gate settings the
        // model rejects. The bound fallback is load-bearing, not defensive: the agent runtime builds its
        // ChatOptions without a ModelId, so the creation-time model is the only signal on that path.
        _apply(_capabilitySettings, effective.ModelId ?? _boundModelId, effective);
        return effective;
    }
}

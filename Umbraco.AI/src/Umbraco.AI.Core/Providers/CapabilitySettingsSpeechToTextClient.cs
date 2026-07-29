using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Speech-to-text client decorator that applies provider-declared capability settings onto each request's
/// <see cref="SpeechToTextOptions"/> before delegating to the inner client.
/// </summary>
/// <remarks>
/// Created by <see cref="AISpeechToTextCapabilityBase{TSettings, TCapabilitySettings}"/> with the resolved,
/// typed capability settings baked in. The caller's <see cref="SpeechToTextOptions"/> instance is never
/// mutated; a per-request copy is used.
/// </remarks>
/// <typeparam name="TCapabilitySettings">The provider-declared capability settings type.</typeparam>
internal sealed class CapabilitySettingsSpeechToTextClient<TCapabilitySettings> : DelegatingSpeechToTextClient
    where TCapabilitySettings : class
{
    private readonly TCapabilitySettings _capabilitySettings;
    private readonly string? _boundModelId;
    private readonly Action<TCapabilitySettings, string?, SpeechToTextOptions> _apply;

    public CapabilitySettingsSpeechToTextClient(
        ISpeechToTextClient innerClient,
        TCapabilitySettings capabilitySettings,
        string? boundModelId,
        Action<TCapabilitySettings, string?, SpeechToTextOptions> apply)
        : base(innerClient)
    {
        _capabilitySettings = capabilitySettings;
        _boundModelId = boundModelId;
        _apply = apply;
    }

    /// <inheritdoc />
    public override Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetTextAsync(audioSpeechStream, Apply(options), cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingTextAsync(audioSpeechStream, Apply(options), cancellationToken);

    private SpeechToTextOptions Apply(SpeechToTextOptions? options)
    {
        // Clone so the caller's options instance is never mutated.
        var effective = options?.Clone() ?? new SpeechToTextOptions();

        // Resolve the model the request will actually run against so the provider can gate settings the
        // model rejects, falling back to the model the client was created for.
        _apply(_capabilitySettings, effective.ModelId ?? _boundModelId, effective);
        return effective;
    }
}

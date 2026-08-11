using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Speech-to-text client decorator that removes the core request options the capability declares the target
/// model does not accept, before delegating to the inner client.
/// </summary>
/// <remarks>
/// The speech-to-text counterpart of <see cref="DeclaredSettingsChatClient"/>. In practice this is the
/// spoken-language hint, which not every transcription model takes.
/// </remarks>
internal sealed class DeclaredSettingsSpeechToTextClient(
    ISpeechToTextClient innerClient,
    IAICapability capability,
    string? boundModelId,
    ILogger? logger)
    : DelegatingSpeechToTextClient(innerClient)
{
    /// <inheritdoc />
    public override Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetTextAsync(audioSpeechStream, Filter(options), cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingTextAsync(audioSpeechStream, Filter(options), cancellationToken);

    private SpeechToTextOptions? Filter(SpeechToTextOptions? options)
    {
        if (options?.SpeechLanguage is null)
        {
            return options;
        }

        var modelId = options.ModelId ?? boundModelId;
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return options;
        }

        if (!capability.GetSettingsSupport(modelId).AsProfileSettingKeys().Contains(AIProfileSettingKeys.Language))
        {
            return options;
        }

        logger?.LogDebug(
            "Model '{ModelId}' declares the language hint unsupported; removed from the request.",
            modelId);

        // Clone so the caller's instance is never mutated.
        var filtered = options.Clone();
        filtered.SpeechLanguage = null;
        return filtered;
    }
}

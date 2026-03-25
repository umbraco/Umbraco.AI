using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Defines an AI speech-to-text service that provides access to audio transcription capabilities.
/// This service acts as a thin layer over Microsoft.Extensions.AI, adding Umbraco-specific
/// features like profiles, connections, and configurable middleware.
/// </summary>
public interface IAISpeechToTextService
{
    /// <summary>
    /// Transcribes audio from a stream using the default speech-to-text profile.
    /// </summary>
    /// <param name="audioStream">The audio stream to transcribe.</param>
    /// <param name="options">Optional speech-to-text options.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The transcription response.</returns>
    Task<SpeechToTextResponse> TranscribeAsync(
        Stream audioStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes audio from a stream using a specific profile.
    /// </summary>
    /// <param name="profileId">The ID of the profile to use.</param>
    /// <param name="audioStream">The audio stream to transcribe.</param>
    /// <param name="options">Optional speech-to-text options.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The transcription response.</returns>
    Task<SpeechToTextResponse> TranscribeAsync(
        Guid profileId,
        Stream audioStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configured speech-to-text client for advanced scenarios.
    /// The returned client has all registered middleware applied and is configured
    /// according to the specified profile.
    /// </summary>
    /// <param name="profileId">Optional profile id. If not specified, uses the default speech-to-text profile.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A configured ISpeechToTextClient instance with middleware applied.</returns>
    Task<ISpeechToTextClient> GetSpeechToTextClientAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default);
}

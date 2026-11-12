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
    /// Transcribes audio using an inline speech-to-text builder with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <param name="configure">Action to configure the inline speech-to-text via the builder.</param>
    /// <param name="audioStream">The audio stream to transcribe.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The transcription response.</returns>
    Task<SpeechToTextResponse> TranscribeAsync(
        Action<AISpeechToTextBuilder> configure,
        Stream audioStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a streaming transcription using an inline speech-to-text builder with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <param name="configure">Action to configure the inline speech-to-text via the builder.</param>
    /// <param name="audioStream">The audio stream to transcribe.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>An async stream of streaming transcription updates.</returns>
    IAsyncEnumerable<SpeechToTextResponseUpdate> StreamTranscriptionAsync(
        Action<AISpeechToTextBuilder> configure,
        Stream audioStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a reusable inline speech-to-text client with scope management per-call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The returned client manages runtime context scopes automatically — each call to
    /// <c>GetTextAsync</c>/<c>GetStreamingTextAsync</c> creates a fresh scope,
    /// sets inline speech-to-text metadata, delegates, and disposes.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> Calling methods on the returned client does not publish
    /// <see cref="AISpeechToTextExecutingNotification"/> or <see cref="AISpeechToTextExecutedNotification"/>.
    /// Use <see cref="TranscribeAsync"/> or <see cref="StreamTranscriptionAsync"/>
    /// for notification support.
    /// </para>
    /// </remarks>
    /// <param name="configure">Action to configure the inline speech-to-text via the builder.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A configured ISpeechToTextClient with inline speech-to-text scope management.</returns>
    Task<ISpeechToTextClient> CreateSpeechToTextClientAsync(
        Action<AISpeechToTextBuilder> configure,
        CancellationToken cancellationToken = default);
}

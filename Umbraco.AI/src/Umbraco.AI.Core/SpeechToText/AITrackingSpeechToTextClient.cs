using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// A speech-to-text client that tracks the last transcription response text.
/// </summary>
/// <param name="innerClient">The inner speech-to-text client to wrap.</param>
internal sealed class AITrackingSpeechToTextClient(ISpeechToTextClient innerClient)
    : AIBoundSpeechToTextClientBase(innerClient)
{
    /// <summary>
    /// The last transcription text received from the speech-to-text client.
    /// </summary>
    public string? LastTranscriptionText { get; private set; }

    /// <inheritdoc />
    public override async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetTextAsync(audioSpeechStream, options, cancellationToken);

        LastTranscriptionText = response.Text;

        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stream = base.GetStreamingTextAsync(audioSpeechStream, options, cancellationToken);

        var textParts = new List<string>();

        await foreach (var update in stream)
        {
            yield return update;

            if (update.Text is not null)
            {
                textParts.Add(update.Text);
            }
        }

        // After streaming completes, aggregate text for audit/usage recording
        LastTranscriptionText = string.Concat(textParts);
    }
}

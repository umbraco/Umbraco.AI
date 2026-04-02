using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Speech-to-text middleware that tracks transcription results for downstream middleware
/// (e.g., auditing and usage recording).
/// </summary>
public sealed class AITrackingSpeechToTextMiddleware : IAISpeechToTextMiddleware
{
    /// <inheritdoc />
    public ISpeechToTextClient Apply(ISpeechToTextClient client)
        => new AITrackingSpeechToTextClient(client);
}

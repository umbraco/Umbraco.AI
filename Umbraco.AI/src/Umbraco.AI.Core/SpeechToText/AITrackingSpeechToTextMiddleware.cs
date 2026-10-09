using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Speech-to-text middleware that records usage analytics and audit entries for transcriptions, via
/// the shared <see cref="IAIOperationTracker"/>.
/// </summary>
internal sealed class AITrackingSpeechToTextMiddleware : IAISpeechToTextMiddleware
{
    private readonly IAIOperationTracker _tracker;

    public AITrackingSpeechToTextMiddleware(IAIOperationTracker tracker)
        => _tracker = tracker;

    /// <inheritdoc />
    public ISpeechToTextClient Apply(ISpeechToTextClient client) => new AITrackingSpeechToTextClient(client, _tracker);
}

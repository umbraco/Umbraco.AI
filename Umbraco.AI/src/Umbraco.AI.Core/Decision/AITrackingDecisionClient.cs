using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;

#pragma warning disable UMBRACOAI_DECISION // IAIDecisionClient is experimental

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Decision client that records usage analytics and audit entries around a decision request, by
/// delegating to the shared <see cref="IAIOperationTracker"/>. One call — however many questions it
/// carries — is one usage record. Mirrors <c>AITrackingSpeechToTextClient</c> — the non-streaming half
/// of it, since <see cref="IAIDecisionClient"/> has no streaming variant.
/// </summary>
internal sealed class AITrackingDecisionClient : IAIDecisionClient
{
    private readonly IAIDecisionClient _innerClient;
    private readonly IAIOperationTracker _tracker;

    public AITrackingDecisionClient(IAIDecisionClient innerClient, IAIOperationTracker tracker)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
    }

    /// <inheritdoc />
    public async Task<AIDecisionResponse> GetResponseAsync(
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var descriptor = BuildDescriptor(request);

        var tracked = await _tracker.TrackAsync(
            descriptor,
            async token =>
            {
                var response = await _innerClient.GetResponseAsync(request, options, token);
                return new AITrackedOperationResult<AIDecisionResponse>
                {
                    Result = response,
                    Usage = response.Usage,
                    ResponseData = BuildResponseData(response),
                };
            },
            cancellationToken);

        return tracked.Result;
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        // Return self if this exact type is requested
        if (serviceType == GetType())
        {
            return this;
        }

        // Delegate to inner client for other services
        return _innerClient.GetService(serviceType, serviceKey);
    }

    /// <inheritdoc />
    public void Dispose() => _innerClient.Dispose();

    private static AIOperationDescriptor BuildDescriptor(AIDecisionRequest request) => new()
    {
        Capability = AICapability.Decision,
        PromptData = BuildPromptData(request),
    };

    /// <summary>
    /// Builds a descriptive prompt data object for audit logging: the shared state plus one entry per
    /// question (id, kind, instructions, and the kind's own criteria/option keys/level descriptions).
    /// </summary>
    private static object BuildPromptData(AIDecisionRequest request) => new
    {
        request.State,
        Questions = request.Questions.Select(BuildQuestionSnapshot).ToList(),
    };

    private static object BuildQuestionSnapshot(AIDecisionQuestion question) => question switch
    {
        AIBinaryDecisionQuestion q => new { q.Id, Kind = "binary", q.Instructions, q.TrueCriteria, q.FalseCriteria },
        AIChoiceDecisionQuestion q => new { q.Id, Kind = "choice", q.Instructions, Options = q.Options.Select(o => o.Key) },
        AIScoreDecisionQuestion q => new { q.Id, Kind = "score", q.Instructions, Levels = q.Levels.Select(l => l.Description) },
        _ => new { question.Id, Kind = question.GetType().Name, question.Instructions },
    };

    /// <summary>
    /// Builds a descriptive response data object for audit logging: one answer per question id.
    /// </summary>
    private static object BuildResponseData(AIDecisionResponse response) => new
    {
        response.ModelId,
        Answers = response.Answers.ToDictionary(kv => kv.Key, kv => BuildAnswerSnapshot(kv.Value)),
    };

    private static object BuildAnswerSnapshot(AIDecisionAnswer answer) => answer switch
    {
        AIBinaryDecisionAnswer a => new { Kind = "binary", a.TrueProbability },
        AIChoiceDecisionAnswer a => new { Kind = "choice", a.Choice, a.Confidence, a.Probabilities },
        AIScoreDecisionAnswer a => new { Kind = "score", a.Score, a.Confidence, a.Probabilities },
        _ => new { Kind = answer.GetType().Name },
    };
}

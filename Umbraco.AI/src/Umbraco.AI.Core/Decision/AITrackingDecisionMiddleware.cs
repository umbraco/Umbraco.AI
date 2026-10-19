using Umbraco.AI.Core.Observability;

#pragma warning disable UMBRACOAI_DECISION // IAIDecisionClient is experimental

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Decision middleware that records usage analytics and audit entries for decisions, via
/// the shared <see cref="IAIOperationTracker"/>.
/// </summary>
internal sealed class AITrackingDecisionMiddleware : IAIDecisionMiddleware
{
    private readonly IAIOperationTracker _tracker;

    public AITrackingDecisionMiddleware(IAIOperationTracker tracker)
        => _tracker = tracker;

    /// <inheritdoc />
    public IAIDecisionClient Apply(IAIDecisionClient client) => new AITrackingDecisionClient(client, _tracker);
}

using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable UMBRACOAI_DECISION // IAIDecisionClient is experimental

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Decision middleware that records usage analytics and audit entries for decisions, via
/// the shared <see cref="IAIOperationTracker"/>.
/// </summary>
internal sealed class AITrackingDecisionMiddleware : IAIDecisionMiddleware
{
    private readonly IAIOperationTracker _tracker;
    private readonly IAIRuntimeContextAccessor _contextAccessor;

    public AITrackingDecisionMiddleware(IAIOperationTracker tracker, IAIRuntimeContextAccessor contextAccessor)
    {
        _tracker = tracker;
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public IAIDecisionClient Apply(IAIDecisionClient client) => new AITrackingDecisionClient(client, _tracker, _contextAccessor);
}

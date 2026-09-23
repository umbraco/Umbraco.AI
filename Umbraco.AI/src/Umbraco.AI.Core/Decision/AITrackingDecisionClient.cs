using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable UMBRACOAI_DECISION // IAIDecisionClient is experimental

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Decision client that records usage analytics and audit entries around a decision request, by
/// delegating to the shared <see cref="IAIOperationTracker"/>. Mirrors
/// <c>AITrackingSpeechToTextClient</c> — the non-streaming half of it, since <see cref="IAIDecisionClient"/>
/// has no streaming variant.
/// </summary>
internal sealed class AITrackingDecisionClient : IAIDecisionClient
{
    private readonly IAIDecisionClient _innerClient;
    private readonly IAIOperationTracker _tracker;
    private readonly IAIRuntimeContextAccessor _contextAccessor;

    public AITrackingDecisionClient(IAIDecisionClient innerClient, IAIOperationTracker tracker, IAIRuntimeContextAccessor contextAccessor)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var descriptor = BuildDescriptor(question);

        var tracked = await _tracker.TrackAsync(
            descriptor,
            async token =>
            {
                var response = await _innerClient.AskAsync(question, options, token);
                return new AITrackedOperationResult<AIDecisionResponse>
                {
                    Result = response,
                    Usage = response.Usage,
                    AuditResponse = new AIAuditResponse { Data = BuildAuditData(response) },
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

    private AIOperationDescriptor BuildDescriptor(AIDecisionQuestion question) => new()
    {
        Capability = AICapability.Decision,
        PromptData = BuildPromptData(question),
        Metadata = AIAuditMetadata.ExtractFromRuntimeContext(_contextAccessor.Context),
        RecordUsageWhenEmpty = true,
    };

    /// <summary>
    /// Builds a descriptive prompt data object for audit logging.
    /// </summary>
    private static object BuildPromptData(AIDecisionQuestion question) => new
    {
        question.Kind,
        question.Prompt,
        question.Choices,
        question.ScoreRange,
    };

    /// <summary>
    /// Builds a descriptive response data object for audit logging.
    /// </summary>
    private static object BuildAuditData(AIDecisionResponse response) => new
    {
        response.Kind,
        response.BinaryAnswer,
        response.SelectedChoice,
        response.Score,
        response.Confidence,
        response.ModelId,
    };
}

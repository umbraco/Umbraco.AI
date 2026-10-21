using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;

#pragma warning disable UMBRACOAI_EXTRACTION // IAIExtractionClient is experimental

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Extraction client that records usage analytics and audit entries around an extraction request, by
/// delegating to the shared <see cref="IAIOperationTracker"/>. One call — however many fields it
/// carries — is one usage record. Mirrors <c>AITrackingSpeechToTextClient</c> — the non-streaming half
/// of it, since <see cref="IAIExtractionClient"/> has no streaming variant.
/// </summary>
internal sealed class AITrackingExtractionClient : IAIExtractionClient
{
    private readonly IAIExtractionClient _innerClient;
    private readonly IAIOperationTracker _tracker;

    public AITrackingExtractionClient(IAIExtractionClient innerClient, IAIOperationTracker tracker)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
    }

    /// <inheritdoc />
    public async Task<AIExtractionResponse> GetResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var descriptor = BuildDescriptor(request);

        var tracked = await _tracker.TrackAsync(
            descriptor,
            async token =>
            {
                var response = await _innerClient.GetResponseAsync(request, options, token);
                return new AITrackedOperationResult<AIExtractionResponse>
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

    private static AIOperationDescriptor BuildDescriptor(AIExtractionRequest request) => new()
    {
        Capability = AICapability.Extraction,
        PromptData = BuildPromptData(request),
    };

    /// <summary>
    /// Builds a descriptive prompt data object for audit logging: the shared state plus one entry per
    /// field (id, kind and instructions).
    /// </summary>
    private static object BuildPromptData(AIExtractionRequest request) => new
    {
        request.State,
        request.ReferenceDate,
        Fields = request.Fields.Select(f => new { f.Id, Kind = ExtractionFieldKind.Of(f), f.Instructions }).ToList(),
    };

    /// <summary>
    /// Builds a descriptive response data object for audit logging: one answer per field id.
    /// </summary>
    private static object BuildResponseData(AIExtractionResponse response) => new
    {
        response.ModelId,
        Answers = response.Answers.ToDictionary(kv => kv.Key, kv => BuildAnswerSnapshot(kv.Value)),
    };

    private static object BuildAnswerSnapshot(AIExtractionAnswer answer) => answer switch
    {
        AITextExtractionAnswer a => new { Kind = ExtractionFieldKind.Of(a), a.Value, a.Confidence, a.NullProbability },
        AINumberExtractionAnswer a => new { Kind = ExtractionFieldKind.Of(a), a.Value, a.Confidence, a.NullProbability },
        AIDateExtractionAnswer a => new { Kind = ExtractionFieldKind.Of(a), a.Value, a.Confidence, a.NullProbability },
        AITimeExtractionAnswer a => new { Kind = ExtractionFieldKind.Of(a), a.Value, a.Confidence, a.NullProbability },
        AIDateTimeExtractionAnswer a => new { Kind = ExtractionFieldKind.Of(a), a.Value, a.Confidence, a.NullProbability },
        AIDurationExtractionAnswer a => new { Kind = ExtractionFieldKind.Of(a), a.Value, a.Unit, a.Confidence, a.NullProbability },
        AIListExtractionAnswer a => new { Kind = ExtractionFieldKind.Of(a), a.Values, a.Confidence, a.NullProbability },
        _ => new { Kind = ExtractionFieldKind.Of(answer) },
    };
}

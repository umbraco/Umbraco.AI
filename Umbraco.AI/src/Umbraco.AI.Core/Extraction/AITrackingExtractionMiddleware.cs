using Umbraco.AI.Core.Observability;

#pragma warning disable UMBRACOAI_EXTRACTION // IAIExtractionClient is experimental

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Extraction middleware that records usage analytics and audit entries for extractions, via
/// the shared <see cref="IAIOperationTracker"/>.
/// </summary>
internal sealed class AITrackingExtractionMiddleware : IAIExtractionMiddleware
{
    private readonly IAIOperationTracker _tracker;

    public AITrackingExtractionMiddleware(IAIOperationTracker tracker)
        => _tracker = tracker;

    /// <inheritdoc />
    public IAIExtractionClient Apply(IAIExtractionClient client) => new AITrackingExtractionClient(client, _tracker);
}

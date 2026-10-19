using Microsoft.Extensions.Logging;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Adds every tracked AI call to the ambient <see cref="AIUsageCollectionScope"/>, if one is open
/// (a test run). Independent of the analytics toggle.
/// </summary>
internal sealed class AITestUsageOperationRecorder : IAIOperationRecorder
{
    private readonly ILogger<AITestUsageOperationRecorder> _logger;

    public AITestUsageOperationRecorder(ILogger<AITestUsageOperationRecorder> logger)
        => _logger = logger;

    public ValueTask<IAIOperationRecording?> BeginAsync(AIOperationStart start, CancellationToken cancellationToken)
        => ValueTask.FromResult<IAIOperationRecording?>(new Recording(this, start));

    private sealed class Recording(AITestUsageOperationRecorder recorder, AIOperationStart start) : IAIOperationRecording
    {
        public ValueTask EndAsync(AIOperationOutcome outcome)
        {
            recorder.Collect(start, outcome);
            return ValueTask.CompletedTask;
        }
    }

    /// <remarks>
    /// Runs on the caller's own flow, because it reads the ambient collector from it.
    /// </remarks>
    private void Collect(AIOperationStart start, AIOperationOutcome outcome)
    {
        try
        {
            var collector = AIUsageCollectionScope.Current;
            if (collector is null)
            {
                return;
            }

            var identity = start.Identity;
            collector.RecordCall(
                start.Descriptor.Capability,
                identity?.ProviderId,
                identity?.ModelId,
                // GetValue<Guid> returns Guid.Empty for a missing key; normalised here rather than in
                // AIUsageContext.ExtractFromRuntimeContext so persisted analytics values don't change.
                identity?.ProfileId == Guid.Empty ? null : identity?.ProfileId,
                identity?.ProfileAlias,
                identity?.FeatureType,
                identity?.FeatureId == Guid.Empty ? null : identity?.FeatureId,
                identity?.FeatureAlias,
                outcome.Usage,
                outcome.DurationMs,
                outcome.Succeeded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect AI usage for {Capability}", start.Descriptor.Capability);
        }
    }
}

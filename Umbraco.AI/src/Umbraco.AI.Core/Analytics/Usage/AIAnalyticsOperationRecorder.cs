using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Observability;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Persists every tracked AI call to usage analytics, when analytics is enabled and the call has
/// something to record.
/// </summary>
internal sealed class AIAnalyticsOperationRecorder : IAIOperationRecorder
{
    private readonly IAIUsageRecordingService _usageRecordingService;
    private readonly IAIUsageRecordFactory _usageRecordFactory;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _analyticsOptions;
    private readonly ILogger<AIAnalyticsOperationRecorder> _logger;

    public AIAnalyticsOperationRecorder(
        IAIUsageRecordingService usageRecordingService,
        IAIUsageRecordFactory usageRecordFactory,
        IOptionsMonitor<AIAnalyticsOptions> analyticsOptions,
        ILogger<AIAnalyticsOperationRecorder> logger)
    {
        _usageRecordingService = usageRecordingService;
        _usageRecordFactory = usageRecordFactory;
        _analyticsOptions = analyticsOptions;
        _logger = logger;
    }

    public ValueTask<IAIOperationRecording?> BeginAsync(AIOperationStart start, CancellationToken cancellationToken)
        => ValueTask.FromResult<IAIOperationRecording?>(new Recording(this, start));

    private sealed class Recording(AIAnalyticsOperationRecorder recorder, AIOperationStart start) : IAIOperationRecording
    {
        public ValueTask EndAsync(AIOperationOutcome outcome)
        {
            // Fire-and-forget: queueing the record must not hold up the AI call's result.
            _ = recorder.RecordAsync(start, outcome);
            return ValueTask.CompletedTask;
        }
    }

    private async Task RecordAsync(AIOperationStart start, AIOperationOutcome outcome)
    {
        try
        {
            if (!_analyticsOptions.CurrentValue.Enabled || start.Identity is null)
            {
                return;
            }

            // Recorded even without token counts (speech-to-text never has them): the duration and
            // status still count.

            var recordContext = AIUsageRecordContext.FromUsageContext(start.Identity);
            var result = new AIUsageRecordResult
            {
                Usage = outcome.Usage,
                DurationMs = outcome.DurationMs,
                Succeeded = outcome.Succeeded,
                Blocked = outcome.Status == AIOperationStatus.Blocked,
                ErrorMessage = outcome.Exception?.Message,
                IsNested = start.IsNested,
            };

            // Not the call's own token: a cancelled call is still a call to record, and the audit log
            // already queues its end status the same way.
            var record = _usageRecordFactory.Create(recordContext, result);
            await _usageRecordingService.QueueRecordUsageAsync(record, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record AI usage for {Capability}", start.Descriptor.Capability);
        }
    }
}

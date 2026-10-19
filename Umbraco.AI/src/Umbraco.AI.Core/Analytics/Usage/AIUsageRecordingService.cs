using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.TaskQueue;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Service for recording raw AI usage data.
/// </summary>
internal sealed class AIUsageRecordingService : IAIUsageRecordingService
{
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<AIUsageRecordingService> _logger;

    public AIUsageRecordingService(
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<AIUsageRecordingService> logger)
    {
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask QueueRecordUsageAsync(AIUsageRecord record, CancellationToken ct = default)
    {
        // All business logic (analytics toggle, validation, user capture) is already done by
        // AIAnalyticsOperationRecorder and the record factory. Queue just the persistence operation.
        var workItem = new BackgroundWorkItem(
            Name: "RecordUsage",
            CorrelationId: record.Id.ToString(),
            RunAsync: async (sp, token) =>
            {
                var repository = sp.GetRequiredService<IAIUsageRecordRepository>();
                await repository.SaveAsync(record, token);
            });

        await _backgroundTaskQueue.QueueAsync(workItem, ct);

        _logger.LogDebug(
            "Queued RecordUsage for {Capability} operation (ID: {RecordId}, Tokens: {TotalTokens}, Duration: {DurationMs}ms)",
            record.Capability,
            record.Id,
            record.TotalTokens,
            record.DurationMs);
    }
}

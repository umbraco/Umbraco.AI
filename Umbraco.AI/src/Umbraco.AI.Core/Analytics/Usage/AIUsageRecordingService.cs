using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.TaskQueue;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Service for recording raw AI usage data.
/// </summary>
internal sealed class AIUsageRecordingService : IAIUsageRecordingService
{
    private readonly IAIUsageRecordRepository _repository;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<AIUsageRecordingService> _logger;

    public AIUsageRecordingService(
        IAIUsageRecordRepository repository,
        IOptionsMonitor<AIAnalyticsOptions> options,
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<AIUsageRecordingService> logger)
    {
        _repository = repository;
        _options = options;
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RecordUsageAsync(AIUsageRecord record, CancellationToken ct = default)
    {
        // Check if analytics is enabled
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("Analytics is disabled, skipping usage recording");
            return;
        }

        try
        {
            await _repository.SaveAsync(record, ct);

            _logger.LogDebug(
                "Recorded usage: {Capability} operation by {UserId} using {ProviderId}/{ModelId} - {TotalTokens} tokens, {DurationMs}ms, Status={Status}",
                record.Capability,
                record.UserId ?? "anonymous",
                record.ProviderId,
                record.ModelId,
                record.TotalTokens,
                record.DurationMs,
                record.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage to database");
            // Don't rethrow - recording failures shouldn't break the main operation
        }
    }

    /// <inheritdoc />
    public async ValueTask QueueRecordUsageAsync(AIUsageRecord record, CancellationToken ct = default)
    {
        // Check if analytics is enabled BEFORE queuing
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("Analytics is disabled, skipping usage recording");
            return;
        }

        // All business logic (validation, user capture) is already done by factory
        // Queue just the persistence operation
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

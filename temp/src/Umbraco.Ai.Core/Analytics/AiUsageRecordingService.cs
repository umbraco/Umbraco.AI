using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.Ai.Core.Analytics;

/// <summary>
/// Service for recording raw AI usage data.
/// </summary>
internal sealed class AiUsageRecordingService : IAiUsageRecordingService
{
    private readonly IAiUsageRecordRepository _repository;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILogger<AiUsageRecordingService> _logger;

    public AiUsageRecordingService(
        IAiUsageRecordRepository repository,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILogger<AiUsageRecordingService> logger)
    {
        _repository = repository;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RecordUsageAsync(AiUsageRecord record, CancellationToken ct = default)
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
}

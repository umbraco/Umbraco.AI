using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.Ai.Core.Analytics;

/// <summary>
/// Background service that periodically cleans up old usage statistics based on retention policies.
/// </summary>
internal sealed class AiUsageStatisticsCleanupJob : BackgroundService
{
    private readonly IAiUsageStatisticsRepository _statisticsRepository;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILogger<AiUsageStatisticsCleanupJob> _logger;

    // Run once per day
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    public AiUsageStatisticsCleanupJob(
        IAiUsageStatisticsRepository statisticsRepository,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILogger<AiUsageStatisticsCleanupJob> logger)
    {
        _statisticsRepository = statisticsRepository;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Usage Statistics Cleanup Job started");

        // Wait a bit on startup to let other services initialize
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_options.CurrentValue.Enabled)
                {
                    _logger.LogDebug("Analytics disabled, skipping cleanup");
                    await Task.Delay(CheckInterval, stoppingToken);
                    continue;
                }

                await CleanupOldStatisticsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cleanup job, will retry on next run");
            }

            // Wait before next cleanup
            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("AI Usage Statistics Cleanup Job stopped");
    }

    /// <summary>
    /// Cleans up hourly and daily statistics older than the configured retention periods.
    /// </summary>
    private async Task CleanupOldStatisticsAsync(CancellationToken ct)
    {
        var options = _options.CurrentValue;
        var now = DateTime.UtcNow;

        // Clean up hourly statistics
        var hourlyRetentionDate = now.AddDays(-options.UsageHourlyRetentionDays);
        _logger.LogInformation(
            "Cleaning up hourly statistics older than {Date} ({Days} days)",
            hourlyRetentionDate,
            options.UsageHourlyRetentionDays);

        try
        {
            await _statisticsRepository.DeleteHourlyOlderThanAsync(hourlyRetentionDate, ct);
            _logger.LogInformation("Completed hourly statistics cleanup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean up hourly statistics");
        }

        // Clean up daily statistics
        var dailyRetentionDate = now.AddDays(-options.UsageDailyRetentionDays);
        _logger.LogInformation(
            "Cleaning up daily statistics older than {Date} ({Days} days)",
            dailyRetentionDate,
            options.UsageDailyRetentionDays);

        try
        {
            await _statisticsRepository.DeleteDailyOlderThanAsync(dailyRetentionDate, ct);
            _logger.LogInformation("Completed daily statistics cleanup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean up daily statistics");
        }

        _logger.LogInformation("Statistics cleanup completed");
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Recurring background job that cleans up old usage statistics based on retention policies.
/// </summary>
internal sealed class AIUsageStatisticsCleanupJob : RecurringBackgroundJobBase
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    private readonly IAIUsageStatisticsRepository _statisticsRepository;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly ILogger<AIUsageStatisticsCleanupJob> _logger;

    public AIUsageStatisticsCleanupJob(
        IAIUsageStatisticsRepository statisticsRepository,
        IOptionsMonitor<AIAnalyticsOptions> options,
        ILogger<AIUsageStatisticsCleanupJob> logger)
        : base(CheckInterval)
    {
        _statisticsRepository = statisticsRepository;
        _options = options;
        _logger = logger;
    }

    public override TimeSpan Delay => StartupDelay;

    public override async Task RunJobAsync(CancellationToken cancellationToken)
    {
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("Analytics disabled, skipping cleanup");
            return;
        }

        await CleanupOldStatisticsAsync(cancellationToken);
    }

    private async Task CleanupOldStatisticsAsync(CancellationToken ct)
    {
        var options = _options.CurrentValue;
        var now = DateTime.UtcNow;

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

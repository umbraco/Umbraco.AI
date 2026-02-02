using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Background service that periodically cleans up old usage statistics based on retention policies.
/// </summary>
internal sealed class AIUsageStatisticsCleanupJob : RecurringHostedServiceBase
{
    private readonly IAIUsageStatisticsRepository _statisticsRepository;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IMainDom _mainDom;
    private readonly ILogger<AIUsageStatisticsCleanupJob> _logger;

    // Run once per day
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    public AIUsageStatisticsCleanupJob(
        IAIUsageStatisticsRepository statisticsRepository,
        IOptionsMonitor<AIAnalyticsOptions> options,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        IMainDom mainDom,
        ILogger<AIUsageStatisticsCleanupJob> logger)
        : base(logger, CheckInterval, StartupDelay)
    {
        _statisticsRepository = statisticsRepository;
        _options = options;
        _runtimeState = runtimeState;
        _serverRoleAccessor = serverRoleAccessor;
        _mainDom = mainDom;
        _logger = logger;
    }

    public override async Task PerformExecuteAsync(object? state)
    {
        // Don't run if analytics is disabled
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("Analytics disabled, skipping cleanup");
            return;
        }

        // Don't run unless Umbraco is running
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        // Don't run on replicas nor unknown role servers
        switch (_serverRoleAccessor.CurrentServerRole)
        {
            case ServerRole.Subscriber:
                _logger.LogDebug("AI Usage Statistics Cleanup will not run on subscriber servers.");
                return;
            case ServerRole.Unknown:
                _logger.LogDebug("AI Usage Statistics Cleanup will not run on servers with unknown role.");
                return;
            case ServerRole.Single:
            case ServerRole.SchedulingPublisher:
            default:
                break;
        }

        // Ensure we do not run if not main domain
        if (!_mainDom.IsMainDom)
        {
            _logger.LogDebug("AI Usage Statistics Cleanup will not run if not MainDom.");
            return;
        }

        await CleanupOldStatisticsAsync(CancellationToken.None);
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

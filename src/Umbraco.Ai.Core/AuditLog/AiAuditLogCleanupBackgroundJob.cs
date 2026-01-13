using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Background service that periodically cleans up old AI audit-log records
/// based on the configured retention period.
/// </summary>
internal sealed class AiAuditLogCleanupBackgroundJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<AiAuditLogOptions> _options;
    private readonly ILogger<AiAuditLogCleanupBackgroundJob> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);

    public AiAuditLogCleanupBackgroundJob(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AiAuditLogOptions> options,
        ILogger<AiAuditLogCleanupBackgroundJob> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Audit Log Cleanup Background Job started. Running every {Interval} hours.",
            _cleanupInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for the cleanup interval
                await Task.Delay(_cleanupInterval, stoppingToken);

                // Check if governance is enabled
                if (!_options.CurrentValue.Enabled)
                {
                    _logger.LogDebug("AI Governance is disabled. Skipping Audit Log cleanup.");
                    continue;
                }

                // Perform cleanup
                await PerformCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during AI Audit Log cleanup");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("AI Audit Log Cleanup Background Job stopped.");
    }

    private async Task PerformCleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var traceService = scope.ServiceProvider.GetRequiredService<IAiAuditLogService>();

        try
        {
            var deleted = await traceService.CleanupOldAuditLogsAsync(stoppingToken);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "AI Audit Log cleanup completed. Deleted {Count} logs older than {Days} days.",
                    deleted, _options.CurrentValue.RetentionDays);
            }
            else
            {
                _logger.LogDebug("AI Audit Log cleanup completed. No old logs to delete.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old audit logs");
            throw;
        }
    }
}

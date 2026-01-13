using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.Ai.Core.Audit;

/// <summary>
/// Background service that periodically cleans up old AI audit records
/// based on the configured retention period.
/// </summary>
internal sealed class AiAuditCleanupBackgroundJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<AiAuditOptions> _options;
    private readonly ILogger<AiAuditCleanupBackgroundJob> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);

    public AiAuditCleanupBackgroundJob(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AiAuditOptions> options,
        ILogger<AiAuditCleanupBackgroundJob> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Audit Cleanup Background Job started. Running every {Interval} hours.",
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
                    _logger.LogDebug("AI Governance is disabled. Skipping audit cleanup.");
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
                _logger.LogError(ex, "Error occurred during AI audit cleanup");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("AI Audit Cleanup Background Job stopped.");
    }

    private async Task PerformCleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var traceService = scope.ServiceProvider.GetRequiredService<IAiAuditService>();

        try
        {
            var deleted = await traceService.CleanupOldAuditsAsync(stoppingToken);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "AI Audit cleanup completed. Deleted {Count} traces older than {Days} days.",
                    deleted, _options.CurrentValue.RetentionDays);
            }
            else
            {
                _logger.LogDebug("AI Audit cleanup completed. No old traces to delete.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old AI traces");
            throw;
        }
    }
}

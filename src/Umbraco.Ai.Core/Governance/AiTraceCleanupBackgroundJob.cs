using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Background service that periodically cleans up old AI trace records
/// based on the configured retention period.
/// </summary>
internal sealed class AiTraceCleanupBackgroundJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<AiGovernanceOptions> _options;
    private readonly ILogger<AiTraceCleanupBackgroundJob> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);

    public AiTraceCleanupBackgroundJob(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AiGovernanceOptions> options,
        ILogger<AiTraceCleanupBackgroundJob> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Trace Cleanup Background Job started. Running every {Interval} hours.",
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
                    _logger.LogDebug("AI Governance is disabled. Skipping trace cleanup.");
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
                _logger.LogError(ex, "Error occurred during AI trace cleanup");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("AI Trace Cleanup Background Job stopped.");
    }

    private async Task PerformCleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var traceService = scope.ServiceProvider.GetRequiredService<IAiTraceService>();

        try
        {
            var deleted = await traceService.CleanupOldTracesAsync(stoppingToken);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "AI Trace cleanup completed. Deleted {Count} traces older than {Days} days.",
                    deleted, _options.CurrentValue.RetentionDays);
            }
            else
            {
                _logger.LogDebug("AI Trace cleanup completed. No old traces to delete.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old AI traces");
            throw;
        }
    }
}

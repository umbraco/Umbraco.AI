using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Recurring background job that cleans up old AI audit-log records based on the configured retention period.
/// </summary>
internal sealed class AIAuditLogCleanupBackgroundJob : RecurringBackgroundJobBase
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<AIAuditLogOptions> _options;
    private readonly ILogger<AIAuditLogCleanupBackgroundJob> _logger;

    public AIAuditLogCleanupBackgroundJob(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AIAuditLogOptions> options,
        ILogger<AIAuditLogCleanupBackgroundJob> logger)
        : base(CleanupInterval)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    public override TimeSpan Delay => StartupDelay;

    public override async Task RunJobAsync(CancellationToken cancellationToken)
    {
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("AI Audit Log is disabled. Skipping Audit Log cleanup.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var traceService = scope.ServiceProvider.GetRequiredService<IAIAuditLogService>();

        try
        {
            var deleted = await traceService.CleanupOldAuditLogsAsync(cancellationToken);

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

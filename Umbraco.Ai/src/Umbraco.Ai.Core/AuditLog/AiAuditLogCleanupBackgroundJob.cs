using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Background service that periodically cleans up old AI audit-log records
/// based on the configured retention period.
/// </summary>
internal sealed class AiAuditLogCleanupBackgroundJob : RecurringHostedServiceBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<AiAuditLogOptions> _options;
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IMainDom _mainDom;
    private readonly ILogger<AiAuditLogCleanupBackgroundJob> _logger;

    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    public AiAuditLogCleanupBackgroundJob(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AiAuditLogOptions> options,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        IMainDom mainDom,
        ILogger<AiAuditLogCleanupBackgroundJob> logger)
        : base(logger, CleanupInterval, StartupDelay)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _runtimeState = runtimeState;
        _serverRoleAccessor = serverRoleAccessor;
        _mainDom = mainDom;
        _logger = logger;
    }

    public override async Task PerformExecuteAsync(object? state)
    {
        // Don't run if governance is disabled
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("AI Audit Log is disabled. Skipping Audit Log cleanup.");
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
                _logger.LogDebug("AI Audit Log cleanup will not run on subscriber servers.");
                return;
            case ServerRole.Unknown:
                _logger.LogDebug("AI Audit Log cleanup will not run on servers with unknown role.");
                return;
            case ServerRole.Single:
            case ServerRole.SchedulingPublisher:
            default:
                break;
        }

        // Ensure we do not run if not main domain
        if (!_mainDom.IsMainDom)
        {
            _logger.LogDebug("AI Audit Log cleanup will not run if not MainDom.");
            return;
        }

        // Perform cleanup
        using var scope = _serviceProvider.CreateScope();
        var traceService = scope.ServiceProvider.GetRequiredService<IAiAuditLogService>();

        try
        {
            var deleted = await traceService.CleanupOldAuditLogsAsync(CancellationToken.None);

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

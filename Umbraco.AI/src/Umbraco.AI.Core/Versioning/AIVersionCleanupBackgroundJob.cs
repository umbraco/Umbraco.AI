using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Umbraco.Ai.Core.Versioning;

/// <summary>
/// Background service that periodically cleans up old entity version records
/// based on the configured cleanup policy.
/// </summary>
internal sealed class AiVersionCleanupBackgroundJob : RecurringHostedServiceBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<AiVersionCleanupPolicy> _options;
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IMainDom _mainDom;
    private readonly ILogger<AiVersionCleanupBackgroundJob> _logger;

    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(12);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(10);

    public AiVersionCleanupBackgroundJob(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AiVersionCleanupPolicy> options,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        IMainDom mainDom,
        ILogger<AiVersionCleanupBackgroundJob> logger)
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
        // Don't run if cleanup is disabled
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("AI Version Cleanup is disabled. Skipping version cleanup.");
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
                _logger.LogDebug("AI Version Cleanup will not run on subscriber servers.");
                return;
            case ServerRole.Unknown:
                _logger.LogDebug("AI Version Cleanup will not run on servers with unknown role.");
                return;
            case ServerRole.Single:
            case ServerRole.SchedulingPublisher:
            default:
                break;
        }

        // Ensure we do not run if not main domain
        if (!_mainDom.IsMainDom)
        {
            _logger.LogDebug("AI Version Cleanup will not run if not MainDom.");
            return;
        }

        // Perform cleanup
        using var scope = _serviceProvider.CreateScope();
        var versionService = scope.ServiceProvider.GetRequiredService<IAiEntityVersionService>();

        try
        {
            var result = await versionService.CleanupVersionsAsync(CancellationToken.None);

            if (result.WasSkipped)
            {
                _logger.LogDebug("AI Version Cleanup was skipped: {Reason}", result.SkipReason);
            }
            else if (result.TotalDeleted > 0)
            {
                _logger.LogInformation(
                    "AI Version Cleanup completed. Deleted {TotalDeleted} versions ({DeletedByAge} by age, {DeletedByCount} by count). {RemainingVersions} versions remaining.",
                    result.TotalDeleted, result.DeletedByAge, result.DeletedByCount, result.RemainingVersions);
            }
            else
            {
                _logger.LogDebug("AI Version Cleanup completed. No old versions to delete. {RemainingVersions} versions remaining.", result.RemainingVersions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old entity versions");
            throw;
        }
    }
}

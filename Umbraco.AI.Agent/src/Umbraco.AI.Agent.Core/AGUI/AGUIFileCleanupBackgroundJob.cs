using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Background service that periodically cleans up expired AG-UI file uploads.
/// Thread directories whose files have not been modified within the retention period are deleted.
/// </summary>
internal sealed class AGUIFileCleanupBackgroundJob : RecurringHostedServiceBase
{
    private readonly IAGUIFileStore _fileStore;
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IMainDom _mainDom;
    private readonly ILogger<AGUIFileCleanupBackgroundJob> _logger;

    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxFileAge = TimeSpan.FromHours(24);

    public AGUIFileCleanupBackgroundJob(
        IAGUIFileStore fileStore,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        IMainDom mainDom,
        ILogger<AGUIFileCleanupBackgroundJob> logger)
        : base(logger, CleanupInterval, StartupDelay)
    {
        _fileStore = fileStore;
        _runtimeState = runtimeState;
        _serverRoleAccessor = serverRoleAccessor;
        _mainDom = mainDom;
        _logger = logger;
    }

    public override async Task PerformExecuteAsync(object? state)
    {
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        switch (_serverRoleAccessor.CurrentServerRole)
        {
            case ServerRole.Subscriber:
            case ServerRole.Unknown:
                return;
        }

        if (!_mainDom.IsMainDom)
        {
            return;
        }

        try
        {
            var deleted = await _fileStore.CleanupExpiredAsync(MaxFileAge, CancellationToken.None);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "AG-UI file cleanup completed. Deleted {Count} expired thread directories.",
                    deleted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired AG-UI files");
            throw;
        }
    }
}

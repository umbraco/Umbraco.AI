using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Agent.Core.Models;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Background service that periodically cleans up expired AG-UI file uploads.
/// Thread directories whose files have not been modified within the configured
/// <see cref="AIAgentOptions.FileRetentionHours"/> period are deleted.
/// </summary>
internal sealed class AGUIFileCleanupBackgroundJob : RecurringHostedServiceBase
{
    private readonly IAGUIFileStore _fileStore;
    private readonly IOptionsMonitor<AIAgentOptions> _options;
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IMainDom _mainDom;
    private readonly ILogger<AGUIFileCleanupBackgroundJob> _logger;

    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    public AGUIFileCleanupBackgroundJob(
        IAGUIFileStore fileStore,
        IOptionsMonitor<AIAgentOptions> options,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        IMainDom mainDom,
        ILogger<AGUIFileCleanupBackgroundJob> logger)
        : base(logger, CleanupInterval, StartupDelay)
    {
        _fileStore = fileStore;
        _options = options;
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
            var maxAge = TimeSpan.FromHours(_options.CurrentValue.FileRetentionHours);
            var deleted = await _fileStore.CleanupExpiredAsync(maxAge, CancellationToken.None);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "AG-UI file cleanup completed. Deleted {Count} expired thread directories (retention: {Hours}h).",
                    deleted, _options.CurrentValue.FileRetentionHours);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired AG-UI files");
            throw;
        }
    }
}

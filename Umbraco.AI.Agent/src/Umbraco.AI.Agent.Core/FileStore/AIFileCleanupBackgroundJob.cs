using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Core.Hosting;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// Background service that periodically cleans up expired file uploads.
/// Thread directories whose files have not been modified within the configured
/// <see cref="AIAgentOptions.FileRetentionHours"/> period are deleted.
/// </summary>
internal sealed class AIFileCleanupBackgroundJob : UmbracoAIRecurringHostedServiceBase
{
    private readonly IAIFileStore _fileStore;
    private readonly IAILegacyPublicFileCleanup _legacyCleanup;
    private readonly IOptionsMonitor<AIAgentOptions> _options;
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IMainDom _mainDom;
    private readonly ILogger<AIFileCleanupBackgroundJob> _logger;

    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    public AIFileCleanupBackgroundJob(
        IAIFileStore fileStore,
        IAILegacyPublicFileCleanup legacyCleanup,
        IOptionsMonitor<AIAgentOptions> options,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        IMainDom mainDom,
        ILogger<AIFileCleanupBackgroundJob> logger)
        : base(logger, CleanupInterval, StartupDelay)
    {
        _fileStore = fileStore;
        _legacyCleanup = legacyCleanup;
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

        // Runs before the retention sweep, and on every pass rather than once, so an install that fails
        // to delete the directory the first time keeps retrying instead of leaving it public. Sits
        // after the role and main-dom guards above so only one node attempts the delete.
        _legacyCleanup.DeleteLegacyFiles();

        try
        {
            var maxAge = TimeSpan.FromHours(_options.CurrentValue.FileRetentionHours);
            var deleted = await _fileStore.CleanupExpiredAsync(maxAge, CancellationToken.None);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "File cleanup completed. Deleted {Count} expired thread directories (retention: {Hours}h).",
                    deleted, _options.CurrentValue.FileRetentionHours);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired files");
            throw;
        }
    }
}

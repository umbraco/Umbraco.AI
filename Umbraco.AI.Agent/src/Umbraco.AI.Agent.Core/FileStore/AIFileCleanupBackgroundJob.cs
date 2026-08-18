using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Agent.Core.Models;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// Recurring background job that cleans up expired file uploads.
/// Thread directories whose files have not been modified within the configured
/// <see cref="AIAgentOptions.FileRetentionHours"/> period are deleted.
/// </summary>
internal sealed class AIFileCleanupBackgroundJob : RecurringBackgroundJobBase
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    private readonly IAIFileStore _fileStore;
    private readonly IAILegacyPublicFileCleanup _legacyCleanup;
    private readonly IOptionsMonitor<AIAgentOptions> _options;
    private readonly ILogger<AIFileCleanupBackgroundJob> _logger;

    public AIFileCleanupBackgroundJob(
        IAIFileStore fileStore,
        IAILegacyPublicFileCleanup legacyCleanup,
        IOptionsMonitor<AIAgentOptions> options,
        ILogger<AIFileCleanupBackgroundJob> logger)
        : base(CleanupInterval)
    {
        _fileStore = fileStore;
        _legacyCleanup = legacyCleanup;
        _options = options;
        _logger = logger;
    }

    public override TimeSpan Delay => StartupDelay;

    public override async Task RunJobAsync(CancellationToken cancellationToken)
    {
        // Runs before the retention sweep, and on every pass rather than once, so an install that fails
        // to delete the directory the first time keeps retrying instead of leaving it public.
        _legacyCleanup.DeleteLegacyFiles();

        try
        {
            var maxAge = TimeSpan.FromHours(_options.CurrentValue.FileRetentionHours);
            var deleted = await _fileStore.CleanupExpiredAsync(maxAge, cancellationToken);

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

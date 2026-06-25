using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Umbraco.AI.Core.Versioning;

/// <summary>
/// Recurring background job that cleans up old entity version records based on the configured cleanup policy.
/// </summary>
internal sealed class AIVersionCleanupBackgroundJob : RecurringBackgroundJobBase
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(12);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<AIVersionCleanupPolicy> _options;
    private readonly ILogger<AIVersionCleanupBackgroundJob> _logger;

    public AIVersionCleanupBackgroundJob(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AIVersionCleanupPolicy> options,
        ILogger<AIVersionCleanupBackgroundJob> logger)
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
            _logger.LogDebug("AI Version Cleanup is disabled. Skipping version cleanup.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var versionService = scope.ServiceProvider.GetRequiredService<IAIEntityVersionService>();

        try
        {
            var result = await versionService.CleanupVersionsAsync(cancellationToken);

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

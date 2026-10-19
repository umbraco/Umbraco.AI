using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Recurring background job that rolls up hourly statistics into daily statistics.
/// Runs hourly, processing completed days and catching up on any missed periods.
/// </summary>
internal sealed class AIUsageDailyRollupJob : RecurringBackgroundJobBase
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    private readonly IAIUsageAggregationService _aggregationService;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly ILogger<AIUsageDailyRollupJob> _logger;

    public AIUsageDailyRollupJob(
        IAIUsageAggregationService aggregationService,
        IOptionsMonitor<AIAnalyticsOptions> options,
        ILogger<AIUsageDailyRollupJob> logger)
        : base(CheckInterval)
    {
        _aggregationService = aggregationService;
        _options = options;
        _logger = logger;
    }

    public override TimeSpan Delay => StartupDelay;

    public override async Task RunJobAsync(CancellationToken cancellationToken)
    {
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("Analytics disabled, skipping daily rollup");
            return;
        }

        // Only process completed days (yesterday and earlier)
        var yesterday = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-1), DateTimeKind.Utc);

        await _aggregationService.RollUpPendingDaysAsync(yesterday, cancellationToken);
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Recurring background job that aggregates raw usage records into hourly statistics.
/// Runs continuously, processing completed hours and catching up on any missed periods.
/// </summary>
internal sealed class AIUsageHourlyAggregationJob : RecurringBackgroundJobBase
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    private readonly IAIUsageAggregationService _aggregationService;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly ILogger<AIUsageHourlyAggregationJob> _logger;

    public AIUsageHourlyAggregationJob(
        IAIUsageAggregationService aggregationService,
        IOptionsMonitor<AIAnalyticsOptions> options,
        ILogger<AIUsageHourlyAggregationJob> logger)
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
            _logger.LogDebug("Analytics disabled, skipping hourly aggregation");
            return;
        }

        // Only process completed hours
        var now = DateTime.UtcNow;
        var lastCompletedHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-1);

        await _aggregationService.AggregatePendingHoursAsync(lastCompletedHour, cancellationToken);
    }
}

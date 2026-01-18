using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Background service that periodically aggregates raw usage records into hourly statistics.
/// Runs continuously, processing completed hours and catching up on any missed periods.
/// </summary>
internal sealed class AiUsageHourlyAggregationJob : RecurringHostedServiceBase
{
    private readonly IAiUsageAggregationService _aggregationService;
    private readonly IAiUsageRecordRepository _recordRepository;
    private readonly IAiUsageStatisticsRepository _statisticsRepository;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IMainDom _mainDom;
    private readonly ILogger<AiUsageHourlyAggregationJob> _logger;

    // Run every 5 minutes
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    public AiUsageHourlyAggregationJob(
        IAiUsageAggregationService aggregationService,
        IAiUsageRecordRepository recordRepository,
        IAiUsageStatisticsRepository statisticsRepository,
        IOptionsMonitor<AiAnalyticsOptions> options,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRoleAccessor,
        IMainDom mainDom,
        ILogger<AiUsageHourlyAggregationJob> logger)
        : base(logger, CheckInterval, StartupDelay)
    {
        _aggregationService = aggregationService;
        _recordRepository = recordRepository;
        _statisticsRepository = statisticsRepository;
        _options = options;
        _runtimeState = runtimeState;
        _serverRoleAccessor = serverRoleAccessor;
        _mainDom = mainDom;
        _logger = logger;
    }

    public override async Task PerformExecuteAsync(object? state)
    {
        // Don't run if analytics is disabled
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogDebug("Analytics disabled, skipping hourly aggregation");
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
                _logger.LogDebug("AI Usage Hourly Aggregation will not run on subscriber servers.");
                return;
            case ServerRole.Unknown:
                _logger.LogDebug("AI Usage Hourly Aggregation will not run on servers with unknown role.");
                return;
            case ServerRole.Single:
            case ServerRole.SchedulingPublisher:
            default:
                break;
        }

        // Ensure we do not run if not main domain
        if (!_mainDom.IsMainDom)
        {
            _logger.LogDebug("AI Usage Hourly Aggregation will not run if not MainDom.");
            return;
        }

        await ProcessMissingHoursAsync(CancellationToken.None);
    }

    /// <summary>
    /// Processes all hours that need aggregation, from last aggregated to current completed hour.
    /// </summary>
    private async Task ProcessMissingHoursAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var currentCompletedHour = GetHourStart(now.AddHours(-1)); // Only process completed hours

        // Get last aggregated period
        var lastAggregatedPeriod = await _statisticsRepository.GetLastAggregatedHourlyPeriodAsync(ct);

        DateTime startFromHour;

        if (lastAggregatedPeriod == null)
        {
            // No previous aggregation - check if we have any raw records
            var firstRecordTimestamp = await _recordRepository.GetLastRecordTimestampAsync(ct);

            if (firstRecordTimestamp == null)
            {
                _logger.LogDebug("No usage records found, nothing to aggregate");
                return;
            }

            // Start from the hour of the first record
            startFromHour = GetHourStart(firstRecordTimestamp.Value);
            _logger.LogInformation(
                "First hourly aggregation: starting from {StartHour} (first record timestamp: {FirstRecord})",
                startFromHour,
                firstRecordTimestamp);
        }
        else
        {
            // Start from next hour after last aggregated
            startFromHour = lastAggregatedPeriod.Value.AddHours(1);
            _logger.LogDebug(
                "Last aggregated hour: {LastHour}, processing from {StartHour}",
                lastAggregatedPeriod,
                startFromHour);
        }

        // Process all missing hours sequentially
        var currentHour = startFromHour;
        var processedCount = 0;

        while (currentHour <= currentCompletedHour && !ct.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Aggregating hour: {Hour}", currentHour);
                await _aggregationService.AggregateHourlyAsync(currentHour, ct);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to aggregate hour {Hour}, will retry on next run",
                    currentHour);

                // Stop processing and retry from this hour on next run
                break;
            }

            currentHour = currentHour.AddHours(1);
        }

        if (processedCount > 0)
        {
            _logger.LogInformation(
                "Processed {Count} hours from {Start} to {End}",
                processedCount,
                startFromHour,
                startFromHour.AddHours(processedCount - 1));
        }
        else if (startFromHour <= currentCompletedHour)
        {
            _logger.LogDebug("No new completed hours to process");
        }
    }

    /// <summary>
    /// Gets the start of the hour for a given timestamp.
    /// </summary>
    private static DateTime GetHourStart(DateTime timestamp)
    {
        return new DateTime(
            timestamp.Year,
            timestamp.Month,
            timestamp.Day,
            timestamp.Hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}

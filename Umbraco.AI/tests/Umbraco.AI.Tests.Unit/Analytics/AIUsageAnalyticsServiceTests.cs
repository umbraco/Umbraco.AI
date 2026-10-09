using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Tests.Unit.Analytics;

/// <summary>
/// #530: the dashboard reads each layer (daily rows, hourly rows, raw records) from where the aggregation
/// jobs have actually got to, so usage is never missing while a job is behind and never counted twice.
/// </summary>
public class AIUsageAnalyticsServiceTests
{
    private static readonly DateTime CurrentHour = HourStart(DateTime.UtcNow);
    private static readonly DateTime Today = CurrentHour.Date;

    private readonly List<AIUsageStatistics> _daily = [];
    private readonly List<AIUsageStatistics> _hourly = [];
    private readonly List<AIUsageRecord> _records = [];

    [Fact]
    public async Task GetSummaryAsync_Daily_CountsEveryLayerOnceFromWhereTheJobsGotTo()
    {
        // Arrange: days up to Today-2 are rolled up. Day -1 is only in hourly rows. The hour after the
        // last aggregated one, and the current hour, are still raw records.
        AddDaily(Today.AddDays(-3), requests: 100);
        AddDaily(Today.AddDays(-2), requests: 100);
        AddHourly(Today.AddDays(-2).AddHours(3), requests: 7);   // already in the Today-2 daily row
        AddHourly(Today.AddDays(-1).AddHours(1), requests: 10);
        AddHourly(Today.AddDays(-1).AddHours(5), requests: 10);  // last aggregated hour
        AddRecord(Today.AddDays(-1).AddHours(6));                // completed, not aggregated yet
        AddRecord(CurrentHour);

        // Act
        var summary = await CreateService().GetSummaryAsync(
            Today.AddDays(-10), DateTime.UtcNow, AIUsagePeriod.Daily);

        // Assert
        summary.TotalRequests.ShouldBe(100 + 100 + 10 + 10 + 1 + 1);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_Daily_PutsHourlyRowsAndRawRecordsInTheirOwnDay()
    {
        // Arrange
        AddDaily(Today.AddDays(-2), requests: 100);
        AddHourly(Today.AddDays(-1).AddHours(5), requests: 10);
        AddRecord(Today.AddDays(-1).AddHours(6));
        AddRecord(CurrentHour);

        // Act
        var points = (await CreateService().GetTimeSeriesAsync(
            Today.AddDays(-10), DateTime.UtcNow, AIUsagePeriod.Daily)).ToList();

        // Assert
        points.Select(p => (p.Timestamp, p.RequestCount)).ShouldBe(
        [
            (Today.AddDays(-2), 100),
            (Today.AddDays(-1), 11),
            (Today, 1),
        ]);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_Hourly_IncludesCompletedHoursNotYetAggregated()
    {
        // Arrange: the job has aggregated up to CurrentHour-3; CurrentHour-2 is complete but still raw.
        AddHourly(CurrentHour.AddHours(-4), requests: 10);
        AddHourly(CurrentHour.AddHours(-3), requests: 10);
        AddRecord(CurrentHour.AddHours(-2));
        AddRecord(CurrentHour);

        // Act
        var points = (await CreateService().GetTimeSeriesAsync(
            CurrentHour.AddHours(-10), DateTime.UtcNow, AIUsagePeriod.Hourly)).ToList();

        // Assert
        points.Select(p => (p.Timestamp, p.RequestCount)).ShouldBe(
        [
            (CurrentHour.AddHours(-4), 10),
            (CurrentHour.AddHours(-3), 10),
            (CurrentHour.AddHours(-2), 1),
            (CurrentHour, 1),
        ]);
    }

    [Fact]
    public async Task GetSummaryAsync_BeforeAnyAggregation_CountsAllRawRecords()
    {
        // Arrange
        AddRecord(Today.AddDays(-2).AddHours(4));
        AddRecord(CurrentHour);

        // Act
        var summary = await CreateService().GetSummaryAsync(
            Today.AddDays(-10), DateTime.UtcNow, AIUsagePeriod.Daily);

        // Assert
        summary.TotalRequests.ShouldBe(2);
    }

    [Fact]
    public async Task GetBreakdownByProfileAsync_RenamedWithinALiveHour_GroupsLikeTheAggregationDoes()
    {
        // Arrange: #565. The profile was renamed within the current hour, which is still raw records.
        AddRecord(CurrentHour, profileAlias: "old-name");
        AddRecord(CurrentHour, profileAlias: "new-name");

        // Act
        var breakdown = (await CreateService().GetBreakdownByProfileAsync(
            CurrentHour.AddHours(-1), DateTime.UtcNow, AIUsagePeriod.Hourly)).ToList();

        // Assert: one row, as the hourly aggregation will produce, named after the latest alias.
        breakdown.Select(b => (b.DimensionName, b.RequestCount)).ShouldBe([("new-name", 2)]);
    }

    [Fact]
    public async Task GetBreakdownByProfileAsync_RenamedAcrossHours_ShowsOneRowWithTheLatestAlias()
    {
        // Arrange
        AddHourly(CurrentHour.AddHours(-3), requests: 5, profileAlias: "old-name");
        AddHourly(CurrentHour.AddHours(-2), requests: 5, profileAlias: "new-name");

        // Act
        var breakdown = (await CreateService().GetBreakdownByProfileAsync(
            CurrentHour.AddHours(-10), DateTime.UtcNow, AIUsagePeriod.Hourly)).ToList();

        // Assert
        breakdown.Select(b => (b.DimensionName, b.RequestCount)).ShouldBe([("new-name", 10)]);
    }

    private AIUsageAnalyticsService CreateService()
    {
        var statistics = new Mock<IAIUsageStatisticsRepository>();
        statistics
            .Setup(x => x.GetDailyByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<AIUsageFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime from, DateTime to, AIUsageFilter? _, CancellationToken _) =>
                _daily.Where(s => s.Period >= from && s.Period < to).ToList());
        statistics
            .Setup(x => x.GetHourlyByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<AIUsageFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime from, DateTime to, AIUsageFilter? _, CancellationToken _) =>
                _hourly.Where(s => s.Period >= from && s.Period < to).ToList());
        statistics
            .Setup(x => x.GetLastAggregatedDailyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _daily.Count == 0 ? null : _daily.Max(s => s.Period));
        statistics
            .Setup(x => x.GetLastAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _hourly.Count == 0 ? null : _hourly.Max(s => s.Period));

        var records = new Mock<IAIUsageRecordRepository>();
        records
            .Setup(x => x.GetRecordsByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime from, DateTime to, CancellationToken _) =>
                _records.Where(r => r.Timestamp >= from && r.Timestamp < to).ToList());

        return new AIUsageAnalyticsService(
            records.Object,
            statistics.Object,
            NullLogger<AIUsageAnalyticsService>.Instance);
    }

    private void AddDaily(DateTime day, int requests) => _daily.Add(Statistics(day, requests));

    private void AddHourly(DateTime hour, int requests, string profileAlias = "profile")
        => _hourly.Add(Statistics(hour, requests, profileAlias));

    private void AddRecord(DateTime timestamp, string profileAlias = "profile") => _records.Add(new AIUsageRecord
    {
        Id = Guid.NewGuid(),
        Timestamp = timestamp,
        Capability = AICapability.Chat,
        ProfileId = Guid.Empty,
        ProfileAlias = profileAlias,
        ProviderId = "openai",
        ModelId = "gpt",
        InputTokens = 1,
        OutputTokens = 1,
        TotalTokens = 2,
        DurationMs = 10,
        Status = AIUsageRecordStatus.Succeeded,
        CreatedAt = timestamp,
    });

    private static AIUsageStatistics Statistics(DateTime period, int requests, string profileAlias = "profile") => new()
    {
        Id = Guid.NewGuid(),
        Period = period,
        ProviderId = "openai",
        ModelId = "gpt",
        ProfileId = Guid.Empty,
        ProfileAlias = profileAlias,
        Capability = AICapability.Chat,
        RequestCount = requests,
        SuccessCount = requests,
        FailureCount = 0,
        InputTokens = requests,
        OutputTokens = requests,
        TotalTokens = requests * 2,
        TotalDurationMs = requests * 10,
        CreatedAt = period,
    };

    private static DateTime HourStart(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, 0, 0, DateTimeKind.Utc);
}

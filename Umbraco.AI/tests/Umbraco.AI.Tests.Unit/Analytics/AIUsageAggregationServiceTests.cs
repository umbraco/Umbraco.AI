using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Tests.Unit.Analytics;

/// <summary>
/// Which hours and days the aggregation picks up. #530: the first aggregation and rollup start from the
/// oldest data, not the newest, and data an earlier first run skipped is picked up again. A day is only
/// rolled up once all its hours are aggregated.
/// </summary>
/// <remarks>
/// An hour counts as aggregated when its raw records are read, and a day when its hourly statistics are.
/// The repositories return nothing by default, so those reads are the only trace each aggregation leaves.
/// </remarks>
public class AIUsageAggregationServiceTests
{
    private static readonly DateTime CurrentHour = new(2026, 10, 8, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LastCompletedHour = CurrentHour.AddHours(-1);
    private static readonly DateTime Today = CurrentHour.Date;
    private static readonly DateTime Yesterday = Today.AddDays(-1);

    private readonly Mock<IAIUsageRecordRepository> _records = new();
    private readonly Mock<IAIUsageStatisticsRepository> _statistics = new();

    [Fact]
    public async Task AggregatePendingHours_FirstRun_StartsFromTheOldestRecord()
    {
        // Arrange: no hourly statistics yet; records span CurrentHour-5 to now.
        _records.Setup(x => x.GetFirstRecordTimestampAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurrentHour.AddHours(-5).AddMinutes(20));

        // Act
        await CreateService().AggregatePendingHoursAsync(LastCompletedHour);

        // Assert
        VerifyHourAggregated(CurrentHour.AddHours(-5), Times.Once());
        VerifyHourAggregated(CurrentHour.AddHours(-1), Times.Once());
        VerifyHourAggregated(CurrentHour, Times.Never());
    }

    [Fact]
    public async Task AggregatePendingHours_AggregatesHoursAnEarlierFirstRunSkipped()
    {
        // Arrange: aggregation started at CurrentHour-10, leaving records behind at -14 and -12.
        // Each aggregation deletes its hour's records, so the oldest record moves forward.
        _statistics.Setup(x => x.GetFirstAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurrentHour.AddHours(-10));
        _statistics.Setup(x => x.GetLastAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurrentHour.AddHours(-1));
        _records.SetupSequence(x => x.GetFirstRecordTimestampAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurrentHour.AddHours(-14).AddMinutes(5))
            .ReturnsAsync(CurrentHour.AddHours(-12).AddMinutes(40))
            .ReturnsAsync(CurrentHour.AddMinutes(2));

        // Act
        await CreateService().AggregatePendingHoursAsync(LastCompletedHour);

        // Assert
        VerifyHourAggregated(CurrentHour.AddHours(-14), Times.Once());
        VerifyHourAggregated(CurrentHour.AddHours(-12), Times.Once());
        _records.Verify(
            x => x.GetRecordsByPeriodAsync(It.Is<DateTime>(h => h > CurrentHour.AddHours(-12)), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AggregatePendingHours_LeftoverRecordThatWontAggregate_DoesNotLoop()
    {
        // Arrange: the oldest record never moves (e.g. the aggregation keeps failing to delete it).
        _statistics.Setup(x => x.GetFirstAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurrentHour.AddHours(-10));
        _statistics.Setup(x => x.GetLastAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurrentHour.AddHours(-1));
        _records.Setup(x => x.GetFirstRecordTimestampAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurrentHour.AddHours(-14));

        // Act
        await CreateService().AggregatePendingHoursAsync(LastCompletedHour);

        // Assert
        VerifyHourAggregated(CurrentHour.AddHours(-14), Times.Once());
    }

    [Fact]
    public async Task RollUpPendingDays_FirstRun_StartsFromTheOldestHourlyStatistic()
    {
        // Arrange
        _statistics.Setup(x => x.GetFirstAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Today.AddDays(-3).AddHours(7));

        // Act
        await CreateService().RollUpPendingDaysAsync(Yesterday);

        // Assert
        VerifyDayRolledUp(Today.AddDays(-3), Times.Once());
        VerifyDayRolledUp(Yesterday, Times.Once());
        VerifyDayRolledUp(Today, Times.Never());
    }

    [Fact]
    public async Task RollUpPendingDays_RollsUpDaysAnEarlierFirstRunSkipped()
    {
        // Arrange: daily rollup started at Today-2, but hourly statistics go back to Today-5.
        _statistics.Setup(x => x.GetFirstAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Today.AddDays(-5).AddHours(9));
        _statistics.Setup(x => x.GetFirstAggregatedDailyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Today.AddDays(-2));
        _statistics.Setup(x => x.GetLastAggregatedDailyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Yesterday);

        // Act
        await CreateService().RollUpPendingDaysAsync(Yesterday);

        // Assert
        VerifyDayRolledUp(Today.AddDays(-5), Times.Once());
        VerifyDayRolledUp(Today.AddDays(-3), Times.Once());
        VerifyDayRolledUp(Today.AddDays(-2), Times.Never());
        VerifyDayRolledUp(Yesterday, Times.Never());
    }

    [Fact]
    public async Task RollUpPendingDays_WaitsForADayWhoseHoursAreNotAllAggregated()
    {
        // Arrange: yesterday's last hour still has raw records (the hourly aggregation hasn't reached
        // it), and the rollup is due for yesterday. Rolling it up now would leave that hour out for good.
        ArrangeRollupDueForYesterday();
        _records.Setup(x => x.GetFirstRecordTimestampAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Yesterday.AddHours(23).AddMinutes(30));

        // Act
        await CreateService().RollUpPendingDaysAsync(Yesterday);

        // Assert
        VerifyDayRolledUp(Yesterday, Times.Never());
    }

    [Fact]
    public async Task RollUpPendingDays_RollsUpADayOnceAllItsHoursAreAggregated()
    {
        // Arrange: the only raw records left are from today, so yesterday is fully in hourly statistics.
        ArrangeRollupDueForYesterday();
        _records.Setup(x => x.GetFirstRecordTimestampAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Today.AddMinutes(10));

        // Act
        await CreateService().RollUpPendingDaysAsync(Yesterday);

        // Assert
        VerifyDayRolledUp(Yesterday, Times.Once());
    }

    [Fact]
    public async Task AggregateHourly_RenamedProfile_SavesOneRowWithTheLatestAlias()
    {
        // Arrange: #565. The profile was renamed during the hour; the dashboard shows the latest alias.
        var hour = CurrentHour.AddHours(-1);
        _records.Setup(x => x.GetRecordsByPeriodAsync(hour, hour.AddHours(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Record(hour.AddMinutes(5), "old-name"), Record(hour.AddMinutes(50), "new-name")]);
        List<AIUsageStatistics>? saved = null;
        _statistics.Setup(x => x.SaveHourlyBatchAsync(It.IsAny<IEnumerable<AIUsageStatistics>>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<AIUsageStatistics> stats, CancellationToken _) => saved = stats.ToList());

        // Act
        await CreateService().AggregateHourlyAsync(hour);

        // Assert
        saved.ShouldNotBeNull();
        saved.Select(s => (s.ProfileAlias, s.RequestCount)).ShouldBe([("new-name", 2)]);
    }

    private static AIUsageRecord Record(DateTime timestamp, string profileAlias) => new()
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
    };

    private void ArrangeRollupDueForYesterday()
    {
        _statistics.Setup(x => x.GetFirstAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Today.AddDays(-3));
        _statistics.Setup(x => x.GetFirstAggregatedDailyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Today.AddDays(-3));
        _statistics.Setup(x => x.GetLastAggregatedDailyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Today.AddDays(-2));
    }

    private void VerifyHourAggregated(DateTime hour, Times times) =>
        _records.Verify(x => x.GetRecordsByPeriodAsync(hour, hour.AddHours(1), It.IsAny<CancellationToken>()), times);

    private void VerifyDayRolledUp(DateTime day, Times times) =>
        _statistics.Verify(
            x => x.GetHourlyByPeriodAsync(day, day.AddDays(1), It.IsAny<AIUsageFilter?>(), It.IsAny<CancellationToken>()),
            times);

    private AIUsageAggregationService CreateService() => new(
        _records.Object,
        _statistics.Object,
        NullLogger<AIUsageAggregationService>.Instance);
}

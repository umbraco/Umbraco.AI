using Microsoft.Extensions.Logging.Abstractions;

using Umbraco.AI.Core.Analytics.Usage;

namespace Umbraco.AI.Tests.Unit.Analytics;

/// <summary>
/// The line where usage statistics switch from daily rows to hourly rows, exposed so other packages can bucket
/// usage into local days the way the Usage tab does, and shared with the reader so the two cannot drift.
/// </summary>
public class AIUsageStatisticsCoverageTests
{
    private static AIUsageStatisticsCoverage Cover(DateTime? firstHourly, DateTime? lastHourly, DateTime? lastDaily)
        => new() { FirstHourlyPeriod = firstHourly, LastHourlyPeriod = lastHourly, LastDailyPeriod = lastDaily };

    private static readonly DateTime Day = new(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void DailyBoundary_WithNoData_IsNull()
        => Cover(null, null, null).DailyBoundary.ShouldBeNull();

    [Fact]
    public void DailyBoundary_WithHourlyRowsOnly_IsNull()
        => Cover(Day.AddHours(3), Day.AddDays(2), null).DailyBoundary.ShouldBeNull();

    [Fact]
    public void DailyBoundary_WithDailyRowsOnly_IsTheDayAfterTheLastOne()
        => Cover(null, null, Day).DailyBoundary.ShouldBe(Day.AddDays(1));

    [Fact]
    public void DailyBoundary_AfterCleanup_IsTheDayAfterTheLastDailyRow()
    {
        // Arrange: hourly rows start well after the last rolled-up day, because the older ones were cleaned up.
        var coverage = Cover(Day.AddDays(5).AddHours(2), Day.AddDays(8), Day);

        // Act / Assert
        coverage.DailyBoundary.ShouldBe(Day.AddDays(1));
    }

    [Fact]
    public void DailyBoundary_WhenTheDailyRollupLags_IsTheDayAfterTheLastDailyRow()
    {
        // Arrange: the rollup stopped at Day, though hourly rows run on for days after it.
        var coverage = Cover(Day.AddDays(1), Day.AddDays(6), Day);

        // Act / Assert: those later days are still read hourly.
        coverage.DailyBoundary.ShouldBe(Day.AddDays(1));
    }

    [Fact]
    public void DailyBoundary_WhenHourlyRowsReachBackBeforeTheLastDailyRow_IsTheFirstWholeHourlyDay()
    {
        // Arrange: retention keeps hourly rows for days that are also rolled up.
        var coverage = Cover(Day.AddDays(-3), Day.AddDays(6), Day);

        // Act / Assert
        coverage.DailyBoundary.ShouldBe(Day.AddDays(-3));
    }

    [Fact]
    public void DailyBoundary_WhenTheOldestHourlyRowIsAPartDayInsideTheRolledUpDays_IsTheNextMidnightAfterIt()
    {
        // Arrange: cleanup stopped part way through a day that is already in a daily row.
        var coverage = Cover(Day.AddDays(-2).AddHours(5), Day.AddDays(3), Day);

        // Act / Assert
        coverage.DailyBoundary.ShouldBe(Day.AddDays(-1));
    }

    [Fact]
    public void DailyBoundary_WhenTheOldestHourlyRowIsAtMidnight_IsThatMidnight()
        => Cover(Day.AddDays(-2), Day.AddDays(3), Day).DailyBoundary.ShouldBe(Day.AddDays(-2));

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, false, true)]
    public async Task GetStatisticsCoverageAsync_ReturnsTheBoundaryTheReaderSplitsTotalsAt(
        bool hasDaily, bool hasHourly, bool cleanedUp, bool hourlyOnly)
    {
        // Arrange
        DateTime? lastDaily = hasDaily ? Day : null;
        DateTime? firstHourly = hasHourly ? (cleanedUp ? Day.AddDays(4).AddHours(7) : Day.AddDays(-1).AddHours(9)) : null;
        DateTime? lastHourly = hasHourly ? Day.AddDays(6) : null;
        var (service, reader) = Create(firstHourly, lastHourly, lastDaily);
        var from = Day.AddYears(-1);
        var to = Day.AddYears(1);

        // Act
        var coverage = await service.GetStatisticsCoverageAsync();
        await reader.GetTotalsAsync(from, to, dimension: null, hourlyOnly, CancellationToken.None);

        // Assert: daily rows end, and hourly rows begin, at the exposed boundary. With no boundary, or when
        // only hours are wanted, there is no daily layer.
        var boundary = hourlyOnly ? DateTime.MinValue : coverage!.DailyBoundary ?? DateTime.MinValue;
        _dailyRanges.ShouldBe(boundary == DateTime.MinValue ? [] : [(from.AddHours(-12), boundary)]);
        if (hasHourly)
        {
            _hourlyRanges.First().From.ShouldBe(boundary == DateTime.MinValue ? from : boundary);
        }
        else
        {
            _hourlyRanges.ShouldBeEmpty();
        }

        coverage!.FirstHourlyPeriod.ShouldBe(firstHourly);
        coverage.LastHourlyPeriod.ShouldBe(lastHourly);
        coverage.LastDailyPeriod.ShouldBe(lastDaily);
    }

    [Fact]
    public async Task GetStatisticsCoverageAsync_WhenTheStoreReturnsUnspecifiedKinds_ReturnsUtcOnEveryValue()
    {
        // Arrange: EF Min/Max hand back Unspecified.
        var unspecified = (DateTime t) => DateTime.SpecifyKind(t, DateTimeKind.Unspecified);
        var (service, _) = Create(unspecified(Day.AddDays(-1).AddHours(9)), unspecified(Day.AddDays(6)), unspecified(Day));

        // Act
        var coverage = await service.GetStatisticsCoverageAsync();

        // Assert
        coverage!.FirstHourlyPeriod!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        coverage.LastHourlyPeriod!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        coverage.LastDailyPeriod!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        coverage.DailyBoundary!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetStatisticsCoverageAsync_WithNoRows_ReturnsAnInstanceWithNoDailyLayer()
    {
        // Arrange
        var (service, _) = Create(null, null, null);

        // Act
        var coverage = await service.GetStatisticsCoverageAsync();

        // Assert: reported, and empty, which differs from not reporting (null).
        coverage.ShouldNotBeNull();
        coverage.DailyBoundary.ShouldBeNull();
    }

    [Fact]
    public async Task GetStatisticsCoverageAsync_OnAnImplementationThatDoesNotOverrideIt_ReturnsNull()
    {
        // Arrange
        IAIUsageAnalyticsService service = new MinimalAnalyticsService();

        // Act
        var coverage = await service.GetStatisticsCoverageAsync();

        // Assert
        coverage.ShouldBeNull();
    }

    private readonly List<(DateTime From, DateTime To)> _dailyRanges = [];
    private readonly List<(DateTime From, DateTime To)> _hourlyRanges = [];

    private (AIUsageAnalyticsService Service, AIUsageStatisticsReader Reader) Create(
        DateTime? firstHourly, DateTime? lastHourly, DateTime? lastDaily)
    {
        var statistics = new Mock<IAIUsageStatisticsRepository>();
        statistics.Setup(x => x.GetFirstAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>())).ReturnsAsync(firstHourly);
        statistics.Setup(x => x.GetLastAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>())).ReturnsAsync(lastHourly);
        statistics.Setup(x => x.GetLastAggregatedDailyPeriodAsync(It.IsAny<CancellationToken>())).ReturnsAsync(lastDaily);
        statistics
            .Setup(x => x.GetDailyTotalsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<AIUsageBreakdownDimension?>(), It.IsAny<CancellationToken>()))
            .Callback((DateTime from, DateTime to, AIUsageBreakdownDimension? _, CancellationToken _) => _dailyRanges.Add((from, to)))
            .ReturnsAsync([]);
        statistics
            .Setup(x => x.GetHourlyTotalsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<AIUsageBreakdownDimension?>(), It.IsAny<CancellationToken>()))
            .Callback((DateTime from, DateTime to, AIUsageBreakdownDimension? _, CancellationToken _) => _hourlyRanges.Add((from, to)))
            .ReturnsAsync([]);

        var records = new Mock<IAIUsageRecordRepository>();
        records
            .Setup(x => x.GetRecordsByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var reader = new AIUsageStatisticsReader(
            records.Object, statistics.Object, NullLogger<AIUsageStatisticsReader>.Instance);
        return (new AIUsageAnalyticsService(reader), reader);
    }

    /// <summary>An implementation written before the coverage member existed.</summary>
    private sealed class MinimalAnalyticsService : IAIUsageAnalyticsService
    {
        public Task<AIUsageSummary> GetSummaryAsync(DateTime from, DateTime to, AIUsagePeriod? requestedGranularity = null, AIUsageFilter? filter = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IEnumerable<AIUsageTimeSeriesPoint>> GetTimeSeriesAsync(DateTime from, DateTime to, AIUsagePeriod? requestedGranularity = null, AIUsageFilter? filter = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IEnumerable<AIUsageBreakdownItem>> GetBreakdownByProviderAsync(DateTime from, DateTime to, AIUsagePeriod? requestedGranularity = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IEnumerable<AIUsageBreakdownItem>> GetBreakdownByModelAsync(DateTime from, DateTime to, AIUsagePeriod? requestedGranularity = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IEnumerable<AIUsageBreakdownItem>> GetBreakdownByProfileAsync(DateTime from, DateTime to, AIUsagePeriod? requestedGranularity = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IEnumerable<AIUsageBreakdownItem>> GetBreakdownByUserAsync(DateTime from, DateTime to, AIUsagePeriod? requestedGranularity = null, CancellationToken ct = default) => throw new NotSupportedException();
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Tests.Unit.Analytics;

/// <summary>
/// The usage overview behind the Analytics.UI dashboard: raw counts bucketed by local day, week or month
/// in the editor's time zone, a comparison range, and breakdown rows.
/// </summary>
public class AIUsageOverviewServiceTests
{
    private static readonly TimeZoneInfo Copenhagen = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    private readonly List<AIUsageStatistics> _daily = [];
    private readonly List<AIUsageStatistics> _hourly = [];
    private readonly List<AIUsageRecord> _records = [];

    [Fact]
    public async Task GetOverviewAsync_InATimeZone_PutsAnHourOnTheLocalDayItStartsOn()
    {
        // Arrange: Copenhagen is UTC+1 in January, so 23:00Z is already the next local day.
        AddHourly(Utc(2026, 1, 14, 22), requests: 1);
        AddHourly(Utc(2026, 1, 14, 23), requests: 10);
        AddHourly(Utc(2026, 1, 15, 23), requests: 100);

        // Act
        var overview = await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 15), Copenhagen);

        // Assert: 23:00Z on the 15th is local midnight of the 16th, outside the range.
        overview.Current.Buckets.Select(b => (b.Start, b.Requests)).ShouldBe(
        [
            (new DateOnly(2026, 1, 14), 1),
            (new DateOnly(2026, 1, 15), 10),
        ]);
        overview.Current.Totals.Requests.ShouldBe(11);
    }

    [Fact]
    public async Task GetOverviewAsync_WithoutATimeZone_UsesUtcDays()
    {
        // Arrange
        AddHourly(Utc(2026, 1, 14, 23), requests: 10);
        AddHourly(Utc(2026, 1, 15, 0), requests: 100);

        // Act
        var overview = await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 15), TimeZoneInfo.Utc);

        // Assert
        overview.Current.Buckets.Select(b => b.Requests).ShouldBe([10, 100]);
    }

    [Fact]
    public async Task GetOverviewAsync_OnTheDayTheClocksGoForward_TheLocalDayHasTwentyThreeHours()
    {
        // Arrange: Copenhagen moves to UTC+2 at 01:00Z on 29 March 2026, so that local day is
        // 23:00Z on the 28th to 22:00Z on the 29th.
        AddHourly(Utc(2026, 3, 28, 22), requests: 1);   // 23:00 on the 28th
        AddHourly(Utc(2026, 3, 28, 23), requests: 10);  // 00:00 on the 29th
        AddHourly(Utc(2026, 3, 29, 21), requests: 100); // 23:00 on the 29th
        AddHourly(Utc(2026, 3, 29, 22), requests: 1000); // 00:00 on the 30th

        // Act
        var overview = await Query(new DateOnly(2026, 3, 28), new DateOnly(2026, 3, 30), Copenhagen);

        // Assert
        overview.Current.Buckets.Select(b => (b.Start, b.Requests)).ShouldBe(
        [
            (new DateOnly(2026, 3, 28), 1),
            (new DateOnly(2026, 3, 29), 110),
            (new DateOnly(2026, 3, 30), 1000),
        ]);
    }

    [Fact]
    public async Task GetOverviewAsync_OnTheDayTheClocksGoBack_TheLocalDayHasTwentyFiveHours()
    {
        // Arrange: Copenhagen moves back to UTC+1 at 01:00Z on 25 October 2026, so that local day is
        // 22:00Z on the 24th to 23:00Z on the 25th.
        AddHourly(Utc(2026, 10, 24, 21), requests: 1);   // 23:00 on the 24th
        AddHourly(Utc(2026, 10, 24, 22), requests: 10);  // 00:00 on the 25th
        AddHourly(Utc(2026, 10, 25, 22), requests: 100); // 23:00 on the 25th
        AddHourly(Utc(2026, 10, 25, 23), requests: 1000); // 00:00 on the 26th

        // Act
        var overview = await Query(new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 26), Copenhagen);

        // Assert
        overview.Current.Buckets.Select(b => (b.Start, b.Requests)).ShouldBe(
        [
            (new DateOnly(2026, 10, 24), 1),
            (new DateOnly(2026, 10, 25), 110),
            (new DateOnly(2026, 10, 26), 1000),
        ]);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByWeek_StartsEachBucketOnTheWeekStartDay()
    {
        // Arrange: Wednesday 14 January to Tuesday 20 January 2026.
        AddHourly(Utc(2026, 1, 14, 10), requests: 1);   // Wednesday
        AddHourly(Utc(2026, 1, 18, 10), requests: 10);  // Sunday
        AddHourly(Utc(2026, 1, 19, 10), requests: 100); // Monday

        // Act
        var monday = await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 20), TimeZoneInfo.Utc, AIUsageGroupBy.Week);
        var sunday = await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 20), TimeZoneInfo.Utc, AIUsageGroupBy.Week, DayOfWeek.Sunday);

        // Assert: the first bucket starts before the range, on the week's first day.
        monday.Current.Buckets.Select(b => (b.Start, b.Requests)).ShouldBe(
        [
            (new DateOnly(2026, 1, 12), 11),
            (new DateOnly(2026, 1, 19), 100),
        ]);
        sunday.Current.Buckets.Select(b => (b.Start, b.Requests)).ShouldBe(
        [
            (new DateOnly(2026, 1, 11), 1),
            (new DateOnly(2026, 1, 18), 110),
        ]);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByMonth_StartsEachBucketOnTheFirst()
    {
        // Arrange
        AddHourly(Utc(2026, 1, 31, 10), requests: 1);
        AddHourly(Utc(2026, 2, 1, 10), requests: 10);

        // Act
        var overview = await Query(new DateOnly(2026, 1, 20), new DateOnly(2026, 3, 2), TimeZoneInfo.Utc, AIUsageGroupBy.Month);

        // Assert: February and March are zero-filled where nothing happened.
        overview.Current.Buckets.Select(b => (b.Start, b.Requests)).ShouldBe(
        [
            (new DateOnly(2026, 1, 1), 1),
            (new DateOnly(2026, 2, 1), 10),
            (new DateOnly(2026, 3, 1), 0),
        ]);
    }

    [Fact]
    public async Task GetOverviewAsync_WithACompareRange_ReturnsItsTotalsAndBucketsToo()
    {
        // Arrange
        AddHourly(Utc(2026, 1, 12, 10), requests: 4, failures: 1);
        AddHourly(Utc(2026, 1, 14, 10), requests: 10);

        // Act
        var overview = await Query(
            new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 15), TimeZoneInfo.Utc,
            compareFrom: new DateOnly(2026, 1, 12), compareTo: new DateOnly(2026, 1, 13));

        // Assert
        overview.Current.Totals.Requests.ShouldBe(10);
        overview.Previous.ShouldNotBeNull();
        overview.Previous!.Totals.Requests.ShouldBe(4);
        overview.Previous.Totals.Failed.ShouldBe(1);
        overview.Previous.Buckets.Select(b => (b.Start, b.Requests)).ShouldBe(
        [
            (new DateOnly(2026, 1, 12), 4),
            (new DateOnly(2026, 1, 13), 0),
        ]);
    }

    [Fact]
    public async Task GetOverviewAsync_WithoutACompareRange_HasNoPrevious()
    {
        // Act
        var overview = await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 15), TimeZoneInfo.Utc);

        // Assert
        overview.Previous.ShouldBeNull();
    }

    [Fact]
    public async Task GetOverviewAsync_WithNoUsage_ReturnsZeroFilledBucketsAndZeroTotals()
    {
        // Act
        var overview = await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 16), Copenhagen);

        // Assert
        overview.Current.Buckets.Count.ShouldBe(3);
        overview.Current.Buckets.ShouldAllBe(b => b.Requests == 0 && b.TotalTokens == 0 && b.CachedInputTokens == null);
        overview.Current.Totals.Requests.ShouldBe(0);
        overview.Current.Totals.CachedInputTokens.ShouldBeNull();
    }

    [Fact]
    public async Task GetOverviewAsync_WithASingleDayRange_ReturnsOneBucket()
    {
        // Act
        var overview = await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 14), Copenhagen);

        // Assert
        overview.Current.Buckets.Select(b => b.Start).ShouldBe([new DateOnly(2026, 1, 14)]);
    }

    [Fact]
    public async Task GetOverviewAsync_AfterHourlyRowsWereCleanedUp_UsesDailyRowsOnceAndOnTheDayTheyFallOn()
    {
        // Arrange: hourly rows are only held from 11 January 05:00Z. Days before that are daily rows;
        // the 11th is already rolled up, so its hourly row must not be counted on top of it.
        AddDaily(Utc(2026, 1, 10, 0), requests: 100);
        AddDaily(Utc(2026, 1, 11, 0), requests: 1000);
        AddHourly(Utc(2026, 1, 11, 5), requests: 7); // already inside the 11th's daily row
        AddHourly(Utc(2026, 1, 12, 8), requests: 10);

        // Act
        var overview = await Query(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 12), Copenhagen);

        // Assert
        overview.Current.Buckets.Select(b => (b.Start, b.Requests)).ShouldBe(
        [
            (new DateOnly(2026, 1, 10), 100),
            (new DateOnly(2026, 1, 11), 1000),
            (new DateOnly(2026, 1, 12), 10),
        ]);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByHour_OnTheOldestDayWithHourlyRows_FillsItsHoursEvenThoughTheDayIsRolledUp()
    {
        // Arrange: hourly rows start on the 4th at 16:00Z and the nightly rollup has already done that day, so
        // the day's daily row exists next to the hourly rows that made it up.
        AddDaily(Utc(2026, 11, 4, 0), requests: 5);
        AddHourly(Utc(2026, 11, 4, 16), requests: 3);
        AddHourly(Utc(2026, 11, 4, 17), requests: 2);
        AddHourly(Utc(2026, 11, 5, 9), requests: 10);

        // Act
        var period = (await Query(new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 4), TimeZoneInfo.Utc, AIUsageGroupBy.Hour)).Current;

        // Assert: the hours with an hourly row are filled, the others stay zero, and the totals are their sum.
        period.HourlyBuckets.Count.ShouldBe(24);
        period.HourlyBuckets[16].Requests.ShouldBe(3);
        period.HourlyBuckets[17].Requests.ShouldBe(2);
        period.HourlyBuckets.Count(b => b.Requests > 0).ShouldBe(2);
        period.Totals.Requests.ShouldBe(5);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByHour_OnADayThatOnlyHasADailyRow_StaysZero()
    {
        // Arrange: the 3rd survives only as a daily row, as hourly rows have been cleaned up before it.
        AddDaily(Utc(2026, 11, 3, 0), requests: 7);
        AddHourly(Utc(2026, 11, 4, 16), requests: 3);

        // Act
        var period = (await Query(new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 3), TimeZoneInfo.Utc, AIUsageGroupBy.Hour)).Current;

        // Assert
        period.HourlyBuckets.ShouldAllBe(b => b.Requests == 0);
        period.Totals.Requests.ShouldBe(0);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByDay_OnTheOldestDayWithHourlyRows_StillUsesItsDailyRowOnce()
    {
        // Arrange: the same data as the hourly case above.
        AddDaily(Utc(2026, 11, 4, 0), requests: 5);
        AddHourly(Utc(2026, 11, 4, 16), requests: 3);
        AddHourly(Utc(2026, 11, 4, 17), requests: 2);
        AddHourly(Utc(2026, 11, 5, 9), requests: 10);

        // Act
        var overview = await Query(new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 5), TimeZoneInfo.Utc);

        // Assert
        overview.Current.Buckets.Select(b => (b.Start, b.Requests)).ShouldBe(
        [
            (new DateOnly(2026, 11, 4), 5),
            (new DateOnly(2026, 11, 5), 10),
        ]);
    }

    [Fact]
    public async Task GetOverviewAsync_IncludesRawRecordsNotAggregatedYet()
    {
        // Arrange: nothing aggregated, one raw record.
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        AddRecord(timestamp);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(timestamp, Copenhagen));

        // Act
        var overview = await Query(today, today, Copenhagen);

        // Assert
        overview.Current.Totals.Requests.ShouldBe(1);
        overview.Current.Buckets.Single().Requests.ShouldBe(1);
    }

    [Fact]
    public async Task GetOverviewAsync_SumsCountsAcrossRows()
    {
        // Arrange
        AddHourly(Utc(2026, 1, 14, 10), requests: 3, failures: 1, nested: 2, cached: 5);
        AddHourly(Utc(2026, 1, 14, 11), requests: 2);

        // Act
        var totals = (await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 14), TimeZoneInfo.Utc)).Current.Totals;

        // Assert: AddHourly sets tokens and duration from the request count.
        totals.Requests.ShouldBe(5);
        totals.NestedRequests.ShouldBe(2);
        totals.Failed.ShouldBe(1);
        totals.Succeeded.ShouldBe(4);
        totals.InputTokens.ShouldBe(5);
        totals.OutputTokens.ShouldBe(5);
        totals.TotalTokens.ShouldBe(10);
        totals.DurationMs.ShouldBe(50);
        totals.CachedInputTokens.ShouldBe(5);
    }

    [Fact]
    public async Task GetOverviewAsync_WithAnEndBeforeTheStart_Throws()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => Query(new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 14), TimeZoneInfo.Utc));
    }

    [Fact]
    public void LocalDayStartUtc_InCopenhagen_IsTheUtcInstantOfLocalMidnight()
    {
        // Act & Assert
        AIUsageOverviewService.LocalDayStartUtc(new DateOnly(2026, 1, 15), Copenhagen).ShouldBe(Utc(2026, 1, 14, 23));
        AIUsageOverviewService.LocalDayStartUtc(new DateOnly(2026, 7, 15), Copenhagen).ShouldBe(Utc(2026, 7, 14, 22));
    }

    [Fact]
    public async Task GetBreakdownPageAsync_ByProfile_SortsPagesAndNamesRows()
    {
        // Arrange
        var small = Guid.NewGuid();
        var big = Guid.NewGuid();
        AddHourly(Utc(2026, 1, 14, 10), requests: 1, profileId: small, profileAlias: "small");
        AddHourly(Utc(2026, 1, 14, 11), requests: 9, profileId: big, profileAlias: "old-big");
        AddHourly(Utc(2026, 1, 14, 12), requests: 9, profileId: big, profileAlias: "big");

        // Act
        var first = await Breakdown(AIUsageBreakdownDimension.Profile, take: 1);
        var second = await Breakdown(AIUsageBreakdownDimension.Profile, skip: 1, take: 1);

        // Assert
        first.Total.ShouldBe(2);
        first.Items.Single().Key.ShouldBe(big.ToString());
        first.Items.Single().Name.ShouldBe("big");
        first.Items.Single().Current.Requests.ShouldBe(18);
        second.Items.Single().Name.ShouldBe("small");
        first.Totals.Requests.ShouldBe(19);
    }

    [Fact]
    public async Task GetBreakdownPageAsync_SortedByNameAscending_OrdersRowsByName()
    {
        // Arrange
        AddHourly(Utc(2026, 1, 14, 10), requests: 5, modelId: "b-model");
        AddHourly(Utc(2026, 1, 14, 10), requests: 1, modelId: "a-model");

        // Act
        var page = await Breakdown(AIUsageBreakdownDimension.Model, sort: AIUsageBreakdownSort.Name, descending: false);

        // Assert
        page.Items.Select(i => i.Key).ShouldBe(["a-model", "b-model"]);
    }

    [Fact]
    public async Task GetBreakdownPageAsync_WithASearch_FiltersRowsButNotTotals()
    {
        // Arrange
        AddHourly(Utc(2026, 1, 14, 10), requests: 5, modelId: "gpt-5");
        AddHourly(Utc(2026, 1, 14, 10), requests: 1, modelId: "claude");

        // Act
        var page = await Breakdown(AIUsageBreakdownDimension.Model, search: "GPT");

        // Assert
        page.Total.ShouldBe(1);
        page.Items.Single().Key.ShouldBe("gpt-5");
        page.Totals.Requests.ShouldBe(6);
    }

    [Fact]
    public async Task GetBreakdownPageAsync_WithACompareRange_GivesEachRowItsPreviousCountsOrZeros()
    {
        // Arrange
        AddHourly(Utc(2026, 1, 14, 10), requests: 5, modelId: "gpt-5");
        AddHourly(Utc(2026, 1, 14, 10), requests: 2, modelId: "new-model");
        AddHourly(Utc(2026, 1, 12, 10), requests: 3, modelId: "gpt-5");

        // Act
        var page = await Breakdown(
            AIUsageBreakdownDimension.Model,
            compareFrom: new DateOnly(2026, 1, 12), compareTo: new DateOnly(2026, 1, 13));

        // Assert
        page.Items.Single(i => i.Key == "gpt-5").Previous!.Requests.ShouldBe(3);
        page.Items.Single(i => i.Key == "new-model").Previous!.Requests.ShouldBe(0);
        page.PreviousTotals!.Requests.ShouldBe(3);
    }

    [Fact]
    public async Task GetBreakdownPageAsync_ByUser_GroupsMissingUsersAsAnonymous()
    {
        // Arrange
        AddHourly(Utc(2026, 1, 14, 10), requests: 2, userId: null);
        AddHourly(Utc(2026, 1, 14, 10), requests: 1, userId: "user-1", userName: "Ada");

        // Act
        var page = await Breakdown(AIUsageBreakdownDimension.User);

        // Assert
        page.Items.Select(i => (i.Key, i.Name)).ShouldBe([("anonymous", null), ("user-1", "Ada")]);
    }

    [Fact]
    public async Task GetBreakdownPageAsync_WithNoUsage_ReturnsAnEmptyPageWithZeroTotals()
    {
        // Act
        var page = await Breakdown(AIUsageBreakdownDimension.Provider);

        // Assert
        page.Total.ShouldBe(0);
        page.Items.ShouldBeEmpty();
        page.Totals.Requests.ShouldBe(0);
        page.PreviousTotals.ShouldBeNull();
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByHour_ReturnsOneBucketPerHourOfTheLocalDayInTheZonesOffset()
    {
        // Arrange: 14 January in Copenhagen (UTC+1) runs 23:00Z on the 13th to 23:00Z on the 14th.
        AddHourly(Utc(2026, 1, 13, 23), requests: 1);
        AddHourly(Utc(2026, 1, 14, 9), requests: 10);
        AddHourly(Utc(2026, 1, 14, 23), requests: 100); // the next local day

        // Act
        var period = (await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 14), Copenhagen, AIUsageGroupBy.Hour)).Current;

        // Assert
        period.Buckets.ShouldBeEmpty();
        period.HourlyBuckets.Count.ShouldBe(24);
        period.HourlyBuckets[0].Start.ShouldBe(new DateTimeOffset(2026, 1, 14, 0, 0, 0, TimeSpan.FromHours(1)));
        period.HourlyBuckets[0].Requests.ShouldBe(1);
        period.HourlyBuckets[10].Start.ShouldBe(new DateTimeOffset(2026, 1, 14, 10, 0, 0, TimeSpan.FromHours(1)));
        period.HourlyBuckets[10].Requests.ShouldBe(10);
        period.HourlyBuckets.Sum(b => b.Requests).ShouldBe(11);
        period.Totals.Requests.ShouldBe(11);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByHour_OnTheDayTheClocksGoForward_HasTwentyThreeBuckets()
    {
        // Arrange: Copenhagen skips 02:00 to 03:00 on 29 March 2026.
        AddHourly(Utc(2026, 3, 29, 0), requests: 1);  // 01:00 local, +01:00
        AddHourly(Utc(2026, 3, 29, 1), requests: 10); // 03:00 local, +02:00

        // Act
        var hours = (await Query(new DateOnly(2026, 3, 29), new DateOnly(2026, 3, 29), Copenhagen, AIUsageGroupBy.Hour)).Current.HourlyBuckets;

        // Assert: there is no 02:00, and the offset changes after 01:00.
        hours.Count.ShouldBe(23);
        hours.Select(h => h.Start.Hour).ShouldBe(Enumerable.Range(0, 2).Concat(Enumerable.Range(3, 21)));
        hours[1].Start.ShouldBe(new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.FromHours(1)));
        hours[1].Requests.ShouldBe(1);
        hours[2].Start.ShouldBe(new DateTimeOffset(2026, 3, 29, 3, 0, 0, TimeSpan.FromHours(2)));
        hours[2].Requests.ShouldBe(10);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByHour_OnTheDayTheClocksGoBack_HasTwentyFiveBucketsAndTheRepeatedHourTwice()
    {
        // Arrange: Copenhagen repeats 02:00 on 25 October 2026.
        AddHourly(Utc(2026, 10, 25, 0), requests: 1);  // 02:00 local, +02:00 (first)
        AddHourly(Utc(2026, 10, 25, 1), requests: 10); // 02:00 local, +01:00 (second)

        // Act
        var hours = (await Query(new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 25), Copenhagen, AIUsageGroupBy.Hour)).Current.HourlyBuckets;

        // Assert
        hours.Count.ShouldBe(25);
        var repeated = hours.Where(h => h.Start.Hour == 2).ToList();
        repeated.Select(h => (h.Start.Offset, h.Requests)).ShouldBe(
        [
            (TimeSpan.FromHours(2), 1),
            (TimeSpan.FromHours(1), 10),
        ]);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByHour_WithACompareRange_ReturnsItsHoursToo()
    {
        // Arrange
        AddHourly(Utc(2026, 1, 13, 5), requests: 4);

        // Act
        var overview = await Query(
            new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 14), TimeZoneInfo.Utc, AIUsageGroupBy.Hour,
            compareFrom: new DateOnly(2026, 1, 13), compareTo: new DateOnly(2026, 1, 13));

        // Assert: both ranges have 24 hours, so hour N lines up with hour N.
        overview.Current.HourlyBuckets.Count.ShouldBe(24);
        overview.Previous!.HourlyBuckets.Count.ShouldBe(24);
        overview.Previous.HourlyBuckets[5].Requests.ShouldBe(4);
        overview.Previous.Totals.Requests.ShouldBe(4);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByHour_WithNoUsage_ReturnsZeroFilledHours()
    {
        // Act
        var period = (await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 15), Copenhagen, AIUsageGroupBy.Hour)).Current;

        // Assert
        period.HourlyBuckets.Count.ShouldBe(48);
        period.HourlyBuckets.ShouldAllBe(h => h.Requests == 0 && h.CachedInputTokens == null);
        period.Totals.Requests.ShouldBe(0);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByHour_IncludesRawRecordsNotAggregatedYet()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        AddRecord(timestamp);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(timestamp, Copenhagen));

        // Act
        var period = (await Query(today, today, Copenhagen, AIUsageGroupBy.Hour)).Current;

        // Assert
        period.Totals.Requests.ShouldBe(1);
        period.HourlyBuckets.Sum(h => h.Requests).ShouldBe(1);
    }

    [Fact]
    public async Task GetOverviewAsync_GroupedByHour_InAHalfHourZone_KeepsTheHoursThatStartInsideTheRange()
    {
        // Arrange: India is UTC+5:30, so 14 January runs 18:30Z on the 13th to 18:30Z on the 14th. The hour
        // that starts at 18:00Z on the 13th starts on the 13th, and the one at 18:00Z on the 14th starts on the 14th.
        var kolkata = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        AddHourly(Utc(2026, 1, 13, 18), requests: 1);
        AddHourly(Utc(2026, 1, 13, 19), requests: 10);   // 00:30 on the 14th: the first bucket
        AddHourly(Utc(2026, 1, 14, 18), requests: 100);  // 23:30 on the 14th: the last bucket
        AddHourly(Utc(2026, 1, 14, 19), requests: 1000); // 00:30 on the 15th

        // Act
        var today = await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 14), kolkata, AIUsageGroupBy.Hour,
            compareFrom: new DateOnly(2026, 1, 13), compareTo: new DateOnly(2026, 1, 13));

        // Assert: 24 buckets each, no hour in both ranges.
        today.Current.HourlyBuckets.Count.ShouldBe(24);
        today.Current.HourlyBuckets[0].Start.ShouldBe(new DateTimeOffset(2026, 1, 14, 0, 30, 0, TimeSpan.FromHours(5.5)));
        today.Current.HourlyBuckets[23].Start.ShouldBe(new DateTimeOffset(2026, 1, 14, 23, 30, 0, TimeSpan.FromHours(5.5)));
        today.Current.Totals.Requests.ShouldBe(110);
        today.Previous!.HourlyBuckets.Count.ShouldBe(24);
        today.Previous.Totals.Requests.ShouldBe(1);
    }

    [Fact]
    public async Task GetOverviewAsync_InAHalfHourZone_HourlyTotalsEqualDailyTotals()
    {
        // Arrange
        var kolkata = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        AddHourly(Utc(2026, 1, 13, 18), requests: 1);
        AddHourly(Utc(2026, 1, 13, 19), requests: 10);
        AddHourly(Utc(2026, 1, 14, 18), requests: 100);
        AddHourly(Utc(2026, 1, 14, 19), requests: 1000);

        // Act
        var hourly = await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 14), kolkata, AIUsageGroupBy.Hour);
        var daily = await Query(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 14), kolkata, AIUsageGroupBy.Day);

        // Assert
        hourly.Current.Totals.Requests.ShouldBe(110);
        daily.Current.Totals.Requests.ShouldBe(110);
        daily.Current.Buckets.Single().Requests.ShouldBe(110);
    }

    private Task<AIUsageOverview> Query(
        DateOnly from,
        DateOnly to,
        TimeZoneInfo timeZone,
        AIUsageGroupBy groupBy = AIUsageGroupBy.Day,
        DayOfWeek weekStart = DayOfWeek.Monday,
        DateOnly? compareFrom = null,
        DateOnly? compareTo = null)
        => CreateService().GetOverviewAsync(new AIUsageOverviewQuery
        {
            From = from,
            To = to,
            TimeZone = timeZone,
            GroupBy = groupBy,
            WeekStart = weekStart,
            CompareFrom = compareFrom,
            CompareTo = compareTo,
        });

    private Task<AIUsageBreakdownPage> Breakdown(
        AIUsageBreakdownDimension dimension,
        AIUsageBreakdownSort sort = AIUsageBreakdownSort.Requests,
        bool descending = true,
        string? search = null,
        int skip = 0,
        int take = 10,
        DateOnly? compareFrom = null,
        DateOnly? compareTo = null)
        => CreateService().GetBreakdownPageAsync(new AIUsageBreakdownQuery
        {
            From = new DateOnly(2026, 1, 14),
            To = new DateOnly(2026, 1, 15),
            TimeZone = TimeZoneInfo.Utc,
            Dimension = dimension,
            Sort = sort,
            Descending = descending,
            Search = search,
            Skip = skip,
            Take = take,
            CompareFrom = compareFrom,
            CompareTo = compareTo,
        });

    private AIUsageOverviewService CreateService()
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
            .Setup(x => x.GetHourlyTotalsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<AIUsageBreakdownDimension?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime from, DateTime to, AIUsageBreakdownDimension? dimension, CancellationToken _) =>
                Sum(_hourly.Where(s => s.Period >= from && s.Period < to), dimension));
        statistics
            .Setup(x => x.GetDailyTotalsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<AIUsageBreakdownDimension?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime from, DateTime to, AIUsageBreakdownDimension? dimension, CancellationToken _) =>
                Sum(_daily.Where(s => s.Period >= from && s.Period < to), dimension));
        statistics
            .Setup(x => x.GetLastAggregatedDailyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _daily.Count == 0 ? null : _daily.Max(s => s.Period));
        statistics
            .Setup(x => x.GetFirstAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _hourly.Count == 0 ? null : _hourly.Min(s => s.Period));
        statistics
            .Setup(x => x.GetLastAggregatedHourlyPeriodAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _hourly.Count == 0 ? null : _hourly.Max(s => s.Period));

        var records = new Mock<IAIUsageRecordRepository>();
        records
            .Setup(x => x.GetRecordsByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime from, DateTime to, CancellationToken _) =>
                _records.Where(r => r.Timestamp >= from && r.Timestamp < to).ToList());

        return new AIUsageOverviewService(
            new AIUsageStatisticsReader(records.Object, statistics.Object, NullLogger<AIUsageStatisticsReader>.Instance));
    }

    /// <summary>What the repository's GROUP BY returns: rows summed per period, or per key and name of a dimension.</summary>
    private static IReadOnlyList<AIUsageStatisticsTotal> Sum(IEnumerable<AIUsageStatistics> rows, AIUsageBreakdownDimension? dimension)
    {
        Func<AIUsageStatistics, (DateTime, string?, string?)> group = dimension switch
        {
            AIUsageBreakdownDimension.Provider => s => (default, s.ProviderId, null),
            AIUsageBreakdownDimension.Model => s => (default, s.ModelId, null),
            AIUsageBreakdownDimension.Profile => s => (default, s.ProfileId.ToString(), s.ProfileAlias),
            AIUsageBreakdownDimension.User => s => (default, s.UserId, s.UserName),
            _ => s => (s.Period, null, null),
        };

        return rows
            .GroupBy(group)
            .Select(g => new AIUsageStatisticsTotal
            {
                Period = g.Max(s => s.Period),
                Key = g.Key.Item2,
                Name = g.Key.Item3,
                RequestCount = g.Sum(s => s.RequestCount),
                NestedRequestCount = g.Sum(s => s.NestedRequestCount),
                SuccessCount = g.Sum(s => s.SuccessCount),
                FailureCount = g.Sum(s => s.FailureCount),
                InputTokens = g.Sum(s => s.InputTokens),
                CachedInputTokens = g.Any(s => s.CachedInputTokens != null) ? g.Sum(s => s.CachedInputTokens ?? 0) : null,
                OutputTokens = g.Sum(s => s.OutputTokens),
                TotalTokens = g.Sum(s => s.TotalTokens),
                TotalDurationMs = g.Sum(s => s.TotalDurationMs),
            })
            .ToList();
    }

    private void AddDaily(DateTime day, int requests) => _daily.Add(Statistics(day, requests));

    private void AddHourly(
        DateTime hour,
        int requests,
        int failures = 0,
        int nested = 0,
        long? cached = null,
        Guid? profileId = null,
        string profileAlias = "profile",
        string modelId = "gpt",
        string? userId = null,
        string? userName = null)
        => _hourly.Add(Statistics(hour, requests, failures, nested, cached, profileId, profileAlias, modelId, userId, userName));

    private void AddRecord(DateTime timestamp) => _records.Add(new AIUsageRecord
    {
        Id = Guid.NewGuid(),
        Timestamp = timestamp,
        Capability = AICapability.Chat,
        ProfileId = Guid.Empty,
        ProfileAlias = "profile",
        ProviderId = "openai",
        ModelId = "gpt",
        InputTokens = 1,
        OutputTokens = 1,
        TotalTokens = 2,
        DurationMs = 10,
        Status = AIUsageRecordStatus.Succeeded,
        CreatedAt = timestamp,
    });

    private static AIUsageStatistics Statistics(
        DateTime period,
        int requests,
        int failures = 0,
        int nested = 0,
        long? cached = null,
        Guid? profileId = null,
        string profileAlias = "profile",
        string modelId = "gpt",
        string? userId = null,
        string? userName = null) => new()
    {
        Id = Guid.NewGuid(),
        Period = period,
        ProviderId = "openai",
        ModelId = modelId,
        ProfileId = profileId ?? Guid.Empty,
        ProfileAlias = profileAlias,
        Capability = AICapability.Chat,
        UserId = userId,
        UserName = userName,
        RequestCount = requests,
        NestedRequestCount = nested,
        SuccessCount = requests - failures,
        FailureCount = failures,
        InputTokens = requests,
        CachedInputTokens = cached,
        OutputTokens = requests,
        TotalTokens = requests * 2,
        TotalDurationMs = requests * 10,
        CreatedAt = period,
    };

    private static DateTime Utc(int year, int month, int day, int hour)
        => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}

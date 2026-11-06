using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Web.Api.Management.Analytics.Usage.Controllers;
using Umbraco.AI.Web.Api.Management.Analytics.Usage.Mapping;
using Umbraco.AI.Web.Api.Management.Analytics.Usage.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Analytics;

/// <summary>
/// The usage overview and breakdown endpoints: input validation answers a 400 problem before the service
/// is called, and a valid request reaches the service with the query the caller meant.
/// </summary>
public class UsageOverviewControllerTests
{
    private static readonly DateOnly From = new(2026, 1, 14);
    private static readonly DateOnly To = new(2026, 1, 20);

    private readonly Mock<IAIUsageOverviewService> _service = new();

    [Fact]
    public async Task GetUsageOverview_WithAValidRequest_PassesTheQueryToTheServiceAndMapsTheResult()
    {
        // Arrange
        AIUsageOverviewQuery? captured = null;
        _service
            .Setup(x => x.GetOverviewAsync(It.IsAny<AIUsageOverviewQuery>(), It.IsAny<CancellationToken>()))
            .Callback((AIUsageOverviewQuery q, CancellationToken _) => captured = q)
            .ReturnsAsync(new AIUsageOverview { Current = Period(requests: 7) });

        // Act
        var result = await Overview().GetUsageOverview(
            From, To, From.AddDays(-7), To.AddDays(-7), "Europe/Copenhagen", UsageGroupByModel.Week, weekStart: 7);

        // Assert
        var model = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<UsageOverviewResponseModel>();
        model.Current.Totals.Requests.ShouldBe(7);
        model.Current.Buckets.Single().Start.ShouldBe(From);
        model.Previous.ShouldBeNull();

        captured.ShouldNotBeNull();
        captured!.From.ShouldBe(From);
        captured.CompareFrom.ShouldBe(From.AddDays(-7));
        captured.TimeZone.Id.ShouldBe("Europe/Copenhagen");
        captured.GroupBy.ShouldBe(AIUsageGroupBy.Week);
        captured.WeekStart.ShouldBe(DayOfWeek.Sunday);
    }

    [Fact]
    public async Task GetUsageOverview_WithoutATimeZone_UsesUtc()
    {
        // Arrange
        AIUsageOverviewQuery? captured = null;
        _service
            .Setup(x => x.GetOverviewAsync(It.IsAny<AIUsageOverviewQuery>(), It.IsAny<CancellationToken>()))
            .Callback((AIUsageOverviewQuery q, CancellationToken _) => captured = q)
            .ReturnsAsync(new AIUsageOverview { Current = Period(requests: 0) });

        // Act
        await Overview().GetUsageOverview(From, To);

        // Assert
        captured!.TimeZone.ShouldBe(TimeZoneInfo.Utc);
        captured.GroupBy.ShouldBe(AIUsageGroupBy.Day);
        captured.WeekStart.ShouldBe(DayOfWeek.Monday);
    }

    [Fact]
    public async Task GetUsageOverview_WithAnUnknownTimeZone_ReturnsAProblem()
    {
        // Act
        var result = await Overview().GetUsageOverview(From, To, timeZone: "Mars/Olympus");

        // Assert
        AssertProblem(result, "Invalid Time Zone");
    }

    [Fact]
    public async Task GetUsageOverview_WithAnEndBeforeTheStart_ReturnsAProblem()
    {
        // Act
        var result = await Overview().GetUsageOverview(To, From);

        // Assert
        AssertProblem(result, "Invalid Date Range");
    }

    [Fact]
    public async Task GetUsageOverview_WithOnlyHalfACompareRange_ReturnsAProblem()
    {
        // Act
        var result = await Overview().GetUsageOverview(From, To, compareFrom: From.AddDays(-7));

        // Assert
        AssertProblem(result, "Invalid Date Range");
    }

    [Fact]
    public async Task GetUsageOverview_WithACompareEndBeforeItsStart_ReturnsAProblem()
    {
        // Act
        var result = await Overview().GetUsageOverview(From, To, compareFrom: From, compareTo: From.AddDays(-1));

        // Assert
        AssertProblem(result, "Invalid Date Range");
    }

    [Fact]
    public async Task GetUsageOverview_WithARangeOverTheCap_ReturnsAProblem()
    {
        // Act
        var result = await Overview().GetUsageOverview(From, From.AddDays(5 * 366));

        // Assert
        AssertProblem(result, "Invalid Date Range");
    }

    [Fact]
    public async Task GetUsageOverview_WithACompareRangeOverTheCap_ReturnsAProblem()
    {
        // Act
        var result = await Overview().GetUsageOverview(From, To, compareFrom: From.AddDays(-5 * 366 - 1), compareTo: From.AddDays(-1));

        // Assert
        AssertProblem(result, "Invalid Date Range");
    }

    [Fact]
    public async Task GetUsageOverview_WithARangeAtTheCap_IsAccepted()
    {
        // Arrange
        _service
            .Setup(x => x.GetOverviewAsync(It.IsAny<AIUsageOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIUsageOverview { Current = Period(requests: 0) });

        // Act
        var result = await Overview().GetUsageOverview(From, From.AddDays(5 * 366 - 1));

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUsageOverview_GroupedByHourOverTwoDays_IsAcceptedAndPassedOn()
    {
        // Arrange
        AIUsageOverviewQuery? captured = null;
        _service
            .Setup(x => x.GetOverviewAsync(It.IsAny<AIUsageOverviewQuery>(), It.IsAny<CancellationToken>()))
            .Callback((AIUsageOverviewQuery q, CancellationToken _) => captured = q)
            .ReturnsAsync(new AIUsageOverview { Current = Period(requests: 0) });

        // Act
        var result = await Overview().GetUsageOverview(From, From.AddDays(1), groupBy: UsageGroupByModel.Hour);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        captured!.GroupBy.ShouldBe(AIUsageGroupBy.Hour);
    }

    [Fact]
    public async Task GetUsageOverview_GroupedByHourOverMoreThanTwoDays_ReturnsAProblem()
    {
        // Act
        var result = await Overview().GetUsageOverview(From, From.AddDays(2), groupBy: UsageGroupByModel.Hour);

        // Assert
        AssertProblem(result, "Invalid Date Range");
    }

    [Fact]
    public async Task GetUsageOverview_GroupedByHourWithACompareRangeOverTwoDays_ReturnsAProblem()
    {
        // Act
        var result = await Overview().GetUsageOverview(
            From, From, compareFrom: From.AddDays(-5), compareTo: From.AddDays(-3), groupBy: UsageGroupByModel.Hour);

        // Assert
        AssertProblem(result, "Invalid Date Range");
    }

    [Fact]
    public async Task GetUsageOverview_GroupedByDayOverMoreThanTwoDays_IsAccepted()
    {
        // Arrange
        _service
            .Setup(x => x.GetOverviewAsync(It.IsAny<AIUsageOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIUsageOverview { Current = Period(requests: 0) });

        // Act
        var result = await Overview().GetUsageOverview(From, From.AddDays(30), groupBy: UsageGroupByModel.Day);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public async Task GetUsageOverview_WithAWeekStartOutsideOneToSeven_ReturnsAProblem(int weekStart)
    {
        // Act
        var result = await Overview().GetUsageOverview(From, To, weekStart: weekStart);

        // Assert
        AssertProblem(result, "Invalid Week Start");
    }

    [Fact]
    public async Task GetUsageOverview_WhenInvalid_NeverCallsTheService()
    {
        // Act
        await Overview().GetUsageOverview(To, From);

        // Assert
        _service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUsageBreakdown_WithAValidRequest_PassesTheQueryToTheServiceAndMapsTheResult()
    {
        // Arrange
        AIUsageBreakdownQuery? captured = null;
        _service
            .Setup(x => x.GetBreakdownPageAsync(It.IsAny<AIUsageBreakdownQuery>(), It.IsAny<CancellationToken>()))
            .Callback((AIUsageBreakdownQuery q, CancellationToken _) => captured = q)
            .ReturnsAsync(new AIUsageBreakdownPage
            {
                Total = 1,
                Items = [new AIUsageBreakdownRow { Key = "gpt", Current = Counts(3), Previous = Counts(0) }],
                Totals = Counts(3),
            });

        // Act
        var result = await Breakdown().GetUsageBreakdown(
            UsageBreakdownDimensionModel.Model, From, To,
            sort: UsageBreakdownSortModel.AverageDuration, sortDirection: UsageSortDirectionModel.Ascending,
            query: "gp", skip: 5, take: 20);

        // Assert
        var model = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<UsageBreakdownResponseModel>();
        model.Total.ShouldBe(1);
        model.Items.Single().Key.ShouldBe("gpt");
        model.Items.Single().Current.Requests.ShouldBe(3);
        model.Items.Single().Previous!.Requests.ShouldBe(0);
        model.PreviousTotals.ShouldBeNull();

        captured!.Dimension.ShouldBe(AIUsageBreakdownDimension.Model);
        captured.Sort.ShouldBe(AIUsageBreakdownSort.AverageDuration);
        captured.Descending.ShouldBeFalse();
        captured.Search.ShouldBe("gp");
        captured.Skip.ShouldBe(5);
        captured.Take.ShouldBe(20);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, 101)]
    public async Task GetUsageBreakdown_WithBadPaging_ReturnsAProblem(int skip, int take)
    {
        // Act
        var result = await Breakdown().GetUsageBreakdown(UsageBreakdownDimensionModel.Model, From, To, skip: skip, take: take);

        // Assert
        AssertProblem(result, "Invalid Paging");
    }

    [Fact]
    public async Task GetUsageBreakdown_WithAnUnknownTimeZone_ReturnsAProblem()
    {
        // Act
        var result = await Breakdown().GetUsageBreakdown(UsageBreakdownDimensionModel.User, From, To, timeZone: "Nope");

        // Assert
        AssertProblem(result, "Invalid Time Zone");
    }

    [Fact]
    public async Task GetUsageBreakdown_WithAnEndBeforeTheStart_ReturnsAProblem()
    {
        // Act
        var result = await Breakdown().GetUsageBreakdown(UsageBreakdownDimensionModel.User, To, From);

        // Assert
        AssertProblem(result, "Invalid Date Range");
    }

    private UsageOverviewController Overview() => new(_service.Object, Mapper());

    private UsageBreakdownRowsController Breakdown() => new(_service.Object, Mapper());

    private static IUmbracoMapper Mapper()
        => new UmbracoMapper(
            new MapDefinitionCollection(() => [new UsageMapDefinition()]),
            global::Umbraco.AI.Tests.Common.TestCoreScopeProvider.Create(),
            NullLogger<UmbracoMapper>.Instance);

    private static void AssertProblem(IActionResult result, string title)
    {
        var problem = result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problem.Title.ShouldBe(title);
    }

    private static AIUsageOverviewPeriod Period(int requests) => new()
    {
        From = From,
        To = To,
        Totals = Counts(requests),
        Buckets = [new AIUsageBucket
        {
            Start = From,
            Requests = requests,
            Succeeded = requests,
            Failed = 0,
            InputTokens = 0,
            OutputTokens = 0,
            TotalTokens = 0,
            DurationMs = 0,
        }],
    };

    private static AIUsageCounts Counts(int requests) => new()
    {
        Requests = requests,
        Succeeded = requests,
        Failed = 0,
        InputTokens = 0,
        OutputTokens = 0,
        TotalTokens = 0,
        DurationMs = 0,
    };
}

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Web.Api.Management.Analytics.Usage.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Analytics.Usage.Controllers;

/// <summary>
/// Controller to get the usage overview: totals and a time series for a range and its comparison range.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class UsageOverviewController : UsageRangeControllerBase
{
    /// <summary>
    /// The longest range, in days, that can be bucketed by hour.
    /// </summary>
    protected const int MaxHourlyRangeDays = 2;

    private readonly IAIUsageOverviewService _overviewService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageOverviewController"/> class.
    /// </summary>
    public UsageOverviewController(IAIUsageOverviewService overviewService, IUmbracoMapper umbracoMapper)
    {
        _overviewService = overviewService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get raw usage counts for a range of local days, the same counts for a comparison range, and a
    /// zero-filled time series of each, bucketed by day, week or month in the given time zone.
    /// </summary>
    /// <param name="from">First local day of the range (yyyy-MM-dd).</param>
    /// <param name="to">Last local day of the range, inclusive (yyyy-MM-dd).</param>
    /// <param name="compareFrom">First local day of the comparison range. Send with <paramref name="compareTo"/>.</param>
    /// <param name="compareTo">Last local day of the comparison range, inclusive.</param>
    /// <param name="timeZone">IANA time zone name the days are local to, e.g. Europe/Copenhagen. Defaults to UTC.</param>
    /// <param name="groupBy">
    /// Bucket size of the time series. Defaults to day. Hour returns <c>hourlyBuckets</c> instead of
    /// <c>buckets</c>, covers the whole local days of the range, and takes ranges of up to 2 local days.
    /// </param>
    /// <param name="weekStart">Day weeks start on when grouping by week, 1 (Monday) to 7 (Sunday). Defaults to 1.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Totals and buckets for the range and the comparison range.</returns>
    [HttpGet("usage-overview")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(UsageOverviewResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsageOverview(
        [BindRequired] DateOnly from,
        [BindRequired] DateOnly to,
        DateOnly? compareFrom = null,
        DateOnly? compareTo = null,
        string? timeZone = null,
        UsageGroupByModel groupBy = UsageGroupByModel.Day,
        int weekStart = 1,
        CancellationToken cancellationToken = default)
    {
        if (ValidateRanges(from, to, compareFrom, compareTo, timeZone, out var resolvedTimeZone) is { } invalid)
        {
            return invalid;
        }

        if (groupBy == UsageGroupByModel.Hour
            && (DaysIn(from, to) > MaxHourlyRangeDays || (compareFrom.HasValue && DaysIn(compareFrom.Value, compareTo!.Value) > MaxHourlyRangeDays)))
        {
            return BadRequest(CreateProblemDetails(
                "Invalid Date Range",
                $"Hourly buckets are for ranges of at most {MaxHourlyRangeDays} days."));
        }

        if (weekStart is < 1 or > 7)
        {
            return BadRequest(CreateProblemDetails(
                "Invalid Week Start",
                "'weekStart' must be from 1 (Monday) to 7 (Sunday)."));
        }

        var overview = await _overviewService.GetOverviewAsync(
            new AIUsageOverviewQuery
            {
                From = from,
                To = to,
                CompareFrom = compareFrom,
                CompareTo = compareTo,
                TimeZone = resolvedTimeZone,
                GroupBy = groupBy switch
                {
                    UsageGroupByModel.Hour => AIUsageGroupBy.Hour,
                    UsageGroupByModel.Week => AIUsageGroupBy.Week,
                    UsageGroupByModel.Month => AIUsageGroupBy.Month,
                    _ => AIUsageGroupBy.Day,
                },
                // ISO weekday numbers: 1 is Monday, 7 is Sunday (DayOfWeek.Sunday is 0).
                WeekStart = (DayOfWeek)(weekStart % 7),
            },
            cancellationToken);

        return Ok(_umbracoMapper.Map<UsageOverviewResponseModel>(overview));
    }
}

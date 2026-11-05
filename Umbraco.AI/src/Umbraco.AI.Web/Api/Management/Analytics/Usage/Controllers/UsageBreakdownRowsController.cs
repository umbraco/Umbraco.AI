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
/// Controller to get a page of usage broken down by provider, model, profile or user.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class UsageBreakdownRowsController : UsageRangeControllerBase
{
    /// <summary>
    /// The most rows one page can hold.
    /// </summary>
    protected const int MaxTake = 100;

    private readonly IAIUsageOverviewService _overviewService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageBreakdownRowsController"/> class.
    /// </summary>
    public UsageBreakdownRowsController(IAIUsageOverviewService overviewService, IUmbracoMapper umbracoMapper)
    {
        _overviewService = overviewService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get one page of raw usage counts per provider, model, profile or user for a range of local days,
    /// with each row's counts for a comparison range.
    /// </summary>
    /// <param name="dimension">What to break usage down by.</param>
    /// <param name="from">First local day of the range (yyyy-MM-dd).</param>
    /// <param name="to">Last local day of the range, inclusive (yyyy-MM-dd).</param>
    /// <param name="compareFrom">First local day of the comparison range. Send with <paramref name="compareTo"/>.</param>
    /// <param name="compareTo">Last local day of the comparison range, inclusive.</param>
    /// <param name="timeZone">IANA time zone name the days are local to, e.g. Europe/Copenhagen. Defaults to UTC.</param>
    /// <param name="sort">What to sort rows by. Defaults to requests.</param>
    /// <param name="sortDirection">The sort direction. Defaults to descending.</param>
    /// <param name="query">Text to find in a row's key or name, ignoring case.</param>
    /// <param name="skip">Rows to skip.</param>
    /// <param name="take">Rows to return, 1 to 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A page of rows with the range's totals.</returns>
    [HttpGet("usage-breakdown")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(UsageBreakdownResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsageBreakdown(
        [BindRequired] UsageBreakdownDimensionModel dimension,
        [BindRequired] DateOnly from,
        [BindRequired] DateOnly to,
        DateOnly? compareFrom = null,
        DateOnly? compareTo = null,
        string? timeZone = null,
        UsageBreakdownSortModel sort = UsageBreakdownSortModel.Requests,
        UsageSortDirectionModel sortDirection = UsageSortDirectionModel.Descending,
        string? query = null,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        if (ValidateRanges(from, to, compareFrom, compareTo, timeZone, out var resolvedTimeZone) is { } invalid)
        {
            return invalid;
        }

        if (skip < 0 || take is < 1 or > MaxTake)
        {
            return BadRequest(CreateProblemDetails(
                "Invalid Paging",
                $"'skip' must be 0 or more and 'take' from 1 to {MaxTake}."));
        }

        var page = await _overviewService.GetBreakdownPageAsync(
            new AIUsageBreakdownQuery
            {
                From = from,
                To = to,
                CompareFrom = compareFrom,
                CompareTo = compareTo,
                TimeZone = resolvedTimeZone,
                Dimension = dimension switch
                {
                    UsageBreakdownDimensionModel.Provider => AIUsageBreakdownDimension.Provider,
                    UsageBreakdownDimensionModel.Model => AIUsageBreakdownDimension.Model,
                    UsageBreakdownDimensionModel.Profile => AIUsageBreakdownDimension.Profile,
                    _ => AIUsageBreakdownDimension.User,
                },
                Sort = sort switch
                {
                    UsageBreakdownSortModel.Name => AIUsageBreakdownSort.Name,
                    UsageBreakdownSortModel.TotalTokens => AIUsageBreakdownSort.TotalTokens,
                    UsageBreakdownSortModel.SuccessRate => AIUsageBreakdownSort.SuccessRate,
                    UsageBreakdownSortModel.AverageDuration => AIUsageBreakdownSort.AverageDuration,
                    _ => AIUsageBreakdownSort.Requests,
                },
                Descending = sortDirection == UsageSortDirectionModel.Descending,
                Search = query,
                Skip = skip,
                Take = take,
            },
            cancellationToken);

        return Ok(_umbracoMapper.Map<UsageBreakdownResponseModel>(page));
    }
}

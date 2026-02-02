using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Analytics.Usage;
using Umbraco.Ai.Web.Api.Management.Analytics.Controllers;
using Umbraco.Ai.Web.Api.Management.Analytics.Usage.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Analytics.Usage.Controllers;

/// <summary>
/// Controller to get usage summary statistics.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UsageSummaryController : AnalyticsControllerBase
{
    private readonly IAiUsageAnalyticsService _analyticsService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageSummaryController"/> class.
    /// </summary>
    public UsageSummaryController(IAiUsageAnalyticsService analyticsService, IUmbracoMapper umbracoMapper)
    {
        _analyticsService = analyticsService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get usage summary statistics for a time period.
    /// </summary>
    /// <param name="from">Start time (inclusive, ISO 8601 format).</param>
    /// <param name="to">End time (exclusive, ISO 8601 format).</param>
    /// <param name="granularity">Optional granularity (Hourly or Daily). If not provided, auto-selected based on date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary statistics for the period.</returns>
    [HttpGet("usage-summary")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(UsageSummaryResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsageSummaryResponseModel>> GetUsageSummary(
        DateTime from,
        DateTime to,
        string? granularity = null,
        CancellationToken cancellationToken = default)
    {
        if (from >= to)
        {
            return BadRequest("'from' must be before 'to'");
        }

        // Parse granularity if provided
        AiUsagePeriod? requestedGranularity = null;
        if (!string.IsNullOrEmpty(granularity))
        {
            if (Enum.TryParse<AiUsagePeriod>(granularity, true, out var parsed))
            {
                requestedGranularity = parsed;
            }
            else
            {
                return BadRequest($"Invalid granularity '{granularity}'. Valid values: Hourly, Daily");
            }
        }

        var summary = await _analyticsService.GetSummaryAsync(from, to, requestedGranularity, null, cancellationToken);
        var responseModel = _umbracoMapper.Map<UsageSummaryResponseModel>(summary);

        return Ok(responseModel);
    }
}

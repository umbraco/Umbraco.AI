using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Analytics;
using Umbraco.Ai.Core.Analytics.Usage;
using Umbraco.Ai.Web.Api.Management.Analytics.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Analytics.Controllers;

/// <summary>
/// Controller to get usage time series data.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UsageTimeSeriesController : AnalyticsControllerBase
{
    private readonly IAiUsageAnalyticsService _analyticsService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageTimeSeriesController"/> class.
    /// </summary>
    public UsageTimeSeriesController(IAiUsageAnalyticsService analyticsService, IUmbracoMapper umbracoMapper)
    {
        _analyticsService = analyticsService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get time series data for a time period.
    /// Each point represents one hour or one day depending on granularity.
    /// </summary>
    /// <param name="from">Start time (inclusive, ISO 8601 format).</param>
    /// <param name="to">End time (exclusive, ISO 8601 format).</param>
    /// <param name="granularity">Optional granularity (Hourly or Daily). If not provided, auto-selected based on date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Time series data points.</returns>
    [HttpGet("usage-time-series")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<UsageTimeSeriesPointModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<UsageTimeSeriesPointModel>>> GetUsageTimeSeries(
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

        var timeSeries = await _analyticsService.GetTimeSeriesAsync(from, to, requestedGranularity, null, cancellationToken);
        var responseModels = _umbracoMapper.MapEnumerable<AiUsageTimeSeriesPoint, UsageTimeSeriesPointModel>(timeSeries);

        return Ok(responseModels);
    }
}

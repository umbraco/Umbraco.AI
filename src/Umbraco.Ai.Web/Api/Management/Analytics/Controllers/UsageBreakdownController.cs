using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Analytics;
using Umbraco.Ai.Web.Api.Management.Analytics.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Analytics.Controllers;

/// <summary>
/// Controller to get usage breakdown by dimension.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UsageBreakdownController : AnalyticsControllerBase
{
    private readonly IAiUsageAnalyticsService _analyticsService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageBreakdownController"/> class.
    /// </summary>
    public UsageBreakdownController(IAiUsageAnalyticsService analyticsService, IUmbracoMapper umbracoMapper)
    {
        _analyticsService = analyticsService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get usage breakdown by provider.
    /// </summary>
    /// <param name="from">Start time (inclusive, ISO 8601 format).</param>
    /// <param name="to">End time (exclusive, ISO 8601 format).</param>
    /// <param name="granularity">Optional granularity (Hourly or Daily). If not provided, auto-selected based on date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Usage breakdown by provider.</returns>
    [HttpGet("usage-by-provider")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<UsageBreakdownItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<UsageBreakdownItemModel>>> GetUsageBreakdownByProvider(
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

        var breakdown = await _analyticsService.GetBreakdownByProviderAsync(from, to, requestedGranularity, cancellationToken);
        var responseModels = _umbracoMapper.MapEnumerable<AiUsageBreakdownItem, UsageBreakdownItemModel>(breakdown);

        return Ok(responseModels);
    }

    /// <summary>
    /// Get usage breakdown by model.
    /// </summary>
    /// <param name="from">Start time (inclusive, ISO 8601 format).</param>
    /// <param name="to">End time (exclusive, ISO 8601 format).</param>
    /// <param name="granularity">Optional granularity (Hourly or Daily). If not provided, auto-selected based on date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Usage breakdown by model.</returns>
    [HttpGet("usage-by-model")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<UsageBreakdownItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<UsageBreakdownItemModel>>> GetUsageBreakdownByModel(
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

        var breakdown = await _analyticsService.GetBreakdownByModelAsync(from, to, requestedGranularity, cancellationToken);
        var responseModels = _umbracoMapper.MapEnumerable<AiUsageBreakdownItem, UsageBreakdownItemModel>(breakdown);

        return Ok(responseModels);
    }

    /// <summary>
    /// Get usage breakdown by profile.
    /// </summary>
    /// <param name="from">Start time (inclusive, ISO 8601 format).</param>
    /// <param name="to">End time (exclusive, ISO 8601 format).</param>
    /// <param name="granularity">Optional granularity (Hourly or Daily). If not provided, auto-selected based on date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Usage breakdown by profile.</returns>
    [HttpGet("usage-by-profile")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<UsageBreakdownItemModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<UsageBreakdownItemModel>>> GetUsageBreakdownByProfile(
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

        var breakdown = await _analyticsService.GetBreakdownByProfileAsync(from, to, requestedGranularity, cancellationToken);
        var responseModels = _umbracoMapper.MapEnumerable<AiUsageBreakdownItem, UsageBreakdownItemModel>(breakdown);

        return Ok(responseModels);
    }
}

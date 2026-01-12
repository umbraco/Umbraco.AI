using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Governance;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Trace.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Trace.Controllers;

/// <summary>
/// Controller to get traces associated with a specific entity (e.g., content item).
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByEntityTraceController : TraceControllerBase
{
    private readonly IAiTraceService _traceService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByEntityTraceController"/> class.
    /// </summary>
    public ByEntityTraceController(IAiTraceService traceService, IUmbracoMapper umbracoMapper)
    {
        _traceService = traceService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get traces associated with a specific entity.
    /// </summary>
    /// <param name="entityId">The entity ID (e.g., content item GUID).</param>
    /// <param name="entityType">The entity type (e.g., "content", "media").</param>
    /// <param name="limit">Maximum number of traces to return (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of traces for the entity.</returns>
    [HttpGet("by-entity/{entityId}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<TraceItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTracesByEntity(
        [FromRoute] string entityId,
        [FromQuery] string entityType = "content",
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var traces = await _traceService.GetEntityHistoryAsync(entityId, entityType, limit, cancellationToken);

        return Ok(_umbracoMapper.MapEnumerable<AiTrace, TraceItemResponseModel>(traces));
    }
}

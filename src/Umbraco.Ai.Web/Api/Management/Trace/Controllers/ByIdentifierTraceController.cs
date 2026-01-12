using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Governance;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Trace.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Trace.Controllers;

/// <summary>
/// Controller to get a trace by local ID or OpenTelemetry TraceId.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdentifierTraceController : TraceControllerBase
{
    private readonly IAiTraceService _traceService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdentifierTraceController"/> class.
    /// </summary>
    public ByIdentifierTraceController(IAiTraceService traceService, IUmbracoMapper umbracoMapper)
    {
        _traceService = traceService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a trace by its local ID or OpenTelemetry TraceId.
    /// </summary>
    /// <param name="identifier">The unique identifier (local GUID or OpenTelemetry TraceId string) of the trace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The trace details.</returns>
    [HttpGet($"{{{nameof(identifier)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TraceDetailResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTraceByIdentifier(
        [FromRoute] TraceIdentifier identifier,
        CancellationToken cancellationToken = default)
    {
        // Resolve the identifier to a local trace ID
        var traceId = await _traceService.TryGetTraceIdAsync(identifier, cancellationToken);
        if (traceId is null)
        {
            return TraceOperationStatusResult(TraceOperationStatus.NotFound);
        }

        // Get the trace with full details
        var trace = await _traceService.GetTraceAsync(traceId.Value, cancellationToken, includeSpans: false);
        if (trace is null)
        {
            return TraceOperationStatusResult(TraceOperationStatus.NotFound);
        }

        return Ok(_umbracoMapper.Map<TraceDetailResponseModel>(trace));
    }
}

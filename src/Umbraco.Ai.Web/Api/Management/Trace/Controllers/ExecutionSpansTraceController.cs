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
/// Controller to get execution spans for a trace.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ExecutionSpansTraceController : TraceControllerBase
{
    private readonly IAiTraceService _traceService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionSpansTraceController"/> class.
    /// </summary>
    public ExecutionSpansTraceController(IAiTraceService traceService, IUmbracoMapper umbracoMapper)
    {
        _traceService = traceService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get execution spans for a trace.
    /// </summary>
    /// <param name="identifier">The unique identifier (local GUID or OpenTelemetry TraceId string) of the trace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution spans for the trace.</returns>
    [HttpGet($"{{{nameof(identifier)}}}/spans")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ExecutionSpanResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExecutionSpans(
        [FromRoute] TraceIdentifier identifier,
        CancellationToken cancellationToken = default)
    {
        // Resolve the identifier to a local trace ID
        var traceId = await _traceService.TryGetTraceIdAsync(identifier, cancellationToken);
        if (traceId is null)
        {
            return TraceOperationStatusResult(TraceOperationStatus.NotFound);
        }

        // Get the execution spans
        var spans = await _traceService.GetExecutionSpansAsync(traceId.Value, cancellationToken);

        return Ok(_umbracoMapper.MapEnumerable<AiExecutionSpan, ExecutionSpanResponseModel>(spans));
    }
}

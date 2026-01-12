using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Governance;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Trace.Controllers;

/// <summary>
/// Controller to delete a trace.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteTraceController : TraceControllerBase
{
    private readonly IAiTraceService _traceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTraceController"/> class.
    /// </summary>
    public DeleteTraceController(IAiTraceService traceService)
    {
        _traceService = traceService;
    }

    /// <summary>
    /// Delete a trace by its local ID or OpenTelemetry TraceId.
    /// </summary>
    /// <param name="identifier">The unique identifier (local GUID or OpenTelemetry TraceId string) of the trace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete($"{{{nameof(identifier)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTrace(
        [FromRoute] TraceIdentifier identifier,
        CancellationToken cancellationToken = default)
    {
        // Resolve the identifier to a local trace ID
        var traceId = await _traceService.TryGetTraceIdAsync(identifier, cancellationToken);
        if (traceId is null)
        {
            return TraceOperationStatusResult(TraceOperationStatus.NotFound);
        }

        var deleted = await _traceService.DeleteTraceAsync(traceId.Value, cancellationToken);
        if (!deleted)
        {
            return TraceOperationStatusResult(TraceOperationStatus.NotFound);
        }

        return NoContent();
    }
}

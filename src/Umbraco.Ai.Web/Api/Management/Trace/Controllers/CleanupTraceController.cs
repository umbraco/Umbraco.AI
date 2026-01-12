using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Governance;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Trace.Controllers;

/// <summary>
/// Controller to clean up old traces based on retention policy.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CleanupTraceController : TraceControllerBase
{
    private readonly IAiTraceService _traceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupTraceController"/> class.
    /// </summary>
    public CleanupTraceController(IAiTraceService traceService)
    {
        _traceService = traceService;
    }

    /// <summary>
    /// Trigger cleanup of old traces based on retention policy configured in settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of traces deleted.</returns>
    [HttpPost("cleanup")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupTraces(CancellationToken cancellationToken = default)
    {
        var deletedCount = await _traceService.CleanupOldTracesAsync(cancellationToken);

        return Ok(deletedCount);
    }
}

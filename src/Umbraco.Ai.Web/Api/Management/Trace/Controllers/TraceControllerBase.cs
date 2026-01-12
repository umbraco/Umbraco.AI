using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Trace.Controllers;

/// <summary>
/// Base controller for Trace management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Trace.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Trace.RouteSegment)]
public abstract class TraceControllerBase : UmbracoAiCoreManagementControllerBase
{
    /// <summary>
    /// Maps a trace operation status to an appropriate action result.
    /// </summary>
    /// <param name="status">The operation status.</param>
    /// <returns>The corresponding action result.</returns>
    protected IActionResult TraceOperationStatusResult(TraceOperationStatus status) =>
        OperationStatusResult(status, problemDetailsBuilder => status switch
        {
            TraceOperationStatus.NotFound => NotFound(problemDetailsBuilder
                .WithTitle("Trace not found")
                .WithDetail("The specified trace does not exist.")
                .Build()),
            TraceOperationStatus.InvalidFilter => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid filter")
                .WithDetail("The specified filter criteria are invalid.")
                .Build()),
            _ => StatusCode(500, problemDetailsBuilder
                .WithTitle("Unknown trace operation status")
                .Build())
        });
}

/// <summary>
/// Represents the possible operation statuses for trace operations.
/// </summary>
public enum TraceOperationStatus
{
    /// <summary>
    /// Trace not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Invalid filter criteria provided.
    /// </summary>
    InvalidFilter
}

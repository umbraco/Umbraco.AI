using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Agent.Web.Api.Management.File.Controllers;

/// <summary>
/// Base controller for file storage endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.File.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.File.RouteSegment)]
public abstract class FileControllerBase : UmbracoAIAgentManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a file.
    /// </summary>
    protected IActionResult FileNotFound() => NotFound(new ProblemDetails
    {
        Title = "File not found",
        Detail = "The specified file could not be found or has expired.",
        Status = StatusCodes.Status404NotFound
    });
}

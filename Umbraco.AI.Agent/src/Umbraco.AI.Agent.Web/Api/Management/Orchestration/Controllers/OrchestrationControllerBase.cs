using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Controllers;

/// <summary>
/// Base controller for orchestration-specific endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Orchestration.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Orchestration.RouteSegment)]
public abstract class OrchestrationControllerBase : UmbracoAIAgentManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for an orchestration.
    /// </summary>
    protected IActionResult OrchestrationNotFound() => NotFound(new ProblemDetails
    {
        Title = "AIOrchestration not found",
        Detail = "The specified orchestration could not be found.",
        Status = StatusCodes.Status404NotFound
    });

    /// <summary>
    /// Returns a 409 Conflict response for duplicate alias.
    /// </summary>
    protected IActionResult AliasAlreadyExists(string alias) => Conflict(new ProblemDetails
    {
        Title = "Alias already exists",
        Detail = $"An orchestration with alias '{alias}' already exists.",
        Status = StatusCodes.Status409Conflict
    });
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Agent.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Base controller for agent-specific endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Agent.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Agent.RouteSegment)]
public abstract class AgentControllerBase : UmbracoAiAgentManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a agent.
    /// </summary>
    protected IActionResult AgentNotFound() => NotFound(new ProblemDetails
    {
        Title = "AiAgent not found",
        Detail = "The specified agent could not be found.",
        Status = StatusCodes.Status404NotFound
    });

    /// <summary>
    /// Returns a 409 Conflict response for duplicate alias.
    /// </summary>
    protected IActionResult AliasAlreadyExists(string alias) => Conflict(new ProblemDetails
    {
        Title = "Alias already exists",
        Detail = $"A agent with alias '{alias}' already exists.",
        Status = StatusCodes.Status409Conflict
    });
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Base controller for agent-specific endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Agent.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Agent.RouteSegment)]
public abstract class AgentControllerBase : UmbracoAIAgentManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a agent.
    /// </summary>
    protected IActionResult AgentNotFound() => NotFound(new ProblemDetails
    {
        Type = "Error",
        Title = "AIAgent not found",
        Detail = "The specified agent could not be found.",
        Status = StatusCodes.Status404NotFound
    });

    /// <summary>
    /// The problem details for an agent the caller may not use: it exists, but is limited to user groups
    /// the caller is not in.
    /// </summary>
    protected static ProblemDetails AgentNotAvailableProblem() => new()
    {
        Type = "Error",
        Title = "Agent not available",
        Detail = "The specified agent is not available to you.",
        Status = StatusCodes.Status403Forbidden
    };

    /// <summary>
    /// Returns a 403 Forbidden response for an agent the caller may not use.
    /// </summary>
    protected IActionResult AgentNotAvailable() => StatusCode(StatusCodes.Status403Forbidden, AgentNotAvailableProblem());

    /// <summary>
    /// Returns a 403 Forbidden response with the reason a change was refused.
    /// </summary>
    protected IActionResult Forbidden(string detail) => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
    {
        Type = "Error",
        Title = "Forbidden",
        Detail = detail,
        Status = StatusCodes.Status403Forbidden
    });

    /// <summary>
    /// Returns the response for an <c>onBehalfOf</c> value that could not be resolved to user groups:
    /// 403 when the caller may not act for it, 400 for everything else.
    /// </summary>
    protected IActionResult OnBehalfOfFailed(AIAgentUserGroupResolutionStatus status)
    {
        if (status == AIAgentUserGroupResolutionStatus.Forbidden)
        {
            return Forbidden("You cannot act on behalf of the specified user.");
        }

        return BadRequest(new ProblemDetails
        {
            Type = "Error",
            Title = "Invalid onBehalfOf",
            Detail = status switch
            {
                AIAgentUserGroupResolutionStatus.NotHandled => "The onBehalfOf kind is not supported.",
                AIAgentUserGroupResolutionStatus.NoUser => "The onBehalfOf value has no user to act for.",
                _ => "The onBehalfOf value must be in the form 'kind:value'."
            },
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Returns a 409 Conflict response for duplicate alias.
    /// </summary>
    protected IActionResult AliasAlreadyExists(string alias) => Conflict(new ProblemDetails
    {
        Type = "Error",
        Title = "Alias already exists",
        Detail = $"A agent with alias '{alias}' already exists.",
        Status = StatusCodes.Status409Conflict
    });
}

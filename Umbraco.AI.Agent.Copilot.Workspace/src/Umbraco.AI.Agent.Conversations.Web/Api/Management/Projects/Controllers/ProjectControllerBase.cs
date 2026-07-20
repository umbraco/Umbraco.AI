using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Agent.Copilot.Workspace.Core;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Controllers;

/// <summary>
/// Base controller for project endpoints. Groups them under the <c>Projects</c> OpenAPI area and roots
/// them at the <c>projects</c> route segment.
/// </summary>
[ApiExplorerSettings(GroupName = CopilotWorkspaceConstants.ManagementApi.Projects.GroupName)]
[UmbracoAIVersionedManagementApiRoute(CopilotWorkspaceConstants.ManagementApi.Projects.RouteSegment)]
public abstract class ProjectControllerBase : CopilotWorkspaceManagementControllerBase
{
    /// <summary>Returns a 404 Not Found response for a project.</summary>
    protected IActionResult ProjectNotFound() => NotFound(new ProblemDetails
    {
        Title = "Project not found",
        Detail = "The specified project could not be found for the current user.",
        Status = StatusCodes.Status404NotFound,
    });
}

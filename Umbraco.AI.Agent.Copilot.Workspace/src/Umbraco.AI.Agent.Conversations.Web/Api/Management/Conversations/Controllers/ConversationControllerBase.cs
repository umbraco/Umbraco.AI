using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Agent.Copilot.Workspace.Core;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Base controller for conversation endpoints. Groups them under the <c>Conversations</c> OpenAPI area
/// and roots them at the <c>conversations</c> route segment.
/// </summary>
[ApiExplorerSettings(GroupName = CopilotWorkspaceConstants.ManagementApi.Conversations.GroupName)]
[UmbracoAIVersionedManagementApiRoute(CopilotWorkspaceConstants.ManagementApi.Conversations.RouteSegment)]
public abstract class ConversationControllerBase : CopilotWorkspaceManagementControllerBase
{
    /// <summary>Returns a 404 Not Found response for a conversation.</summary>
    protected IActionResult ConversationNotFound() => NotFound(new ProblemDetails
    {
        Title = "Conversation not found",
        Detail = "The specified conversation could not be found for the current user.",
        Status = StatusCodes.Status404NotFound,
    });
}

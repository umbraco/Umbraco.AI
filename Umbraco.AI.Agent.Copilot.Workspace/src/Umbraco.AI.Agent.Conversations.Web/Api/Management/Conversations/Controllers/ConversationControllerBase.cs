using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Base controller for conversation endpoints. Groups them under the <c>Conversations</c> OpenAPI area
/// and roots them at the <c>conversations</c> route segment.
/// </summary>
[ApiExplorerSettings(GroupName = ConversationsManagementApiConstants.Conversations.GroupName)]
[UmbracoAIVersionedManagementApiRoute(ConversationsManagementApiConstants.Conversations.RouteSegment)]
public abstract class ConversationControllerBase : ConversationsManagementControllerBase
{
    /// <summary>Returns a 404 Not Found response for a conversation.</summary>
    protected IActionResult ConversationNotFound() => NotFound(new ProblemDetails
    {
        Type = "Error",
        Title = "Conversation not found",
        Detail = "The specified conversation could not be found for the current user.",
        Status = StatusCodes.Status404NotFound,
    });

    /// <summary>
    /// Returns a 400 Bad Request response for a save that a saving-notification handler cancelled,
    /// carrying the reason the handler gave.
    /// </summary>
    /// <param name="exception">The cancellation raised by the conversation service.</param>
    private protected IActionResult ConversationSaveCancelled(AIConversationSaveCancelledException exception)
        => BadRequest(new ProblemDetails
        {
            Type = "Error",
            Title = "Conversation save cancelled",
            Detail = exception.Reason,
            Status = StatusCodes.Status400BadRequest,
        });
}

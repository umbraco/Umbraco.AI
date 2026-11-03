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

    /// <summary>
    /// Returns a 400 Bad Request response for a Snapshot share that couldn't be anchored to a message.
    /// This is a deliberate rejection of the share request — distinct from <see cref="ConversationNotFound"/>,
    /// which covers "not owned"/"doesn't exist" — so it must be caught ahead of (not instead of) the
    /// general <see cref="InvalidOperationException"/> catch a caller also has for the ownership check,
    /// since <see cref="AIConversationShareCutoffUnresolvableException"/> derives from it.
    /// </summary>
    /// <param name="exception">The cutoff-resolution failure raised by the share service.</param>
    private protected IActionResult ConversationShareCutoffUnresolvable(AIConversationShareCutoffUnresolvableException exception)
        => BadRequest(new ProblemDetails
        {
            Type = "Error",
            Title = "Conversation share cutoff unresolvable",
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest,
        });

    /// <summary>Returns a 400 Bad Request response for a share request naming an unrecognized scope mode.</summary>
    /// <param name="scopeMode">The unrecognized value the caller sent.</param>
    private protected IActionResult ConversationShareScopeModeInvalid(string scopeMode)
        => BadRequest(new ProblemDetails
        {
            Type = "Error",
            Title = "Invalid scope mode",
            Detail = $"'{scopeMode}' is not a recognized conversation share scope mode. Use 'Snapshot' or 'Live'.",
            Status = StatusCodes.Status400BadRequest,
        });
}

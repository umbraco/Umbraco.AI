using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Drops everything after a conversation's last user message.
/// </summary>
[ApiVersion("1.0")]
public class TruncateConversationMessagesController : ConversationControllerBase
{
    private readonly IAIConversationService _conversationService;

    /// <summary>Initializes a new instance of the <see cref="TruncateConversationMessagesController"/> class.</summary>
    public TruncateConversationMessagesController(IAIConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    /// <summary>
    /// Deletes the trailing assistant/tool block of the conversation's most recent turn, leaving the user
    /// message that prompted it. This is the server-side half of the chat's regenerate action: the client
    /// calls this, truncates its own thread to match, and then starts an ordinary AG-UI run, so the next
    /// answer replaces the previous one instead of being appended after it.
    /// </summary>
    /// <param name="id">The conversation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content, or 404 if not found for the current user.</returns>
    [HttpDelete("{id:guid}/messages/after-last-user")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TruncateAfterLastUserMessage(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _conversationService.TruncateAfterLastUserMessageAsync(id, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return ConversationNotFound();
        }

        return NoContent();
    }
}

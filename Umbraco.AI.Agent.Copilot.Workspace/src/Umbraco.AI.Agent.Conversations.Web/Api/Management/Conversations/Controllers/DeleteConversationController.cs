using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Deletes a conversation.
/// </summary>
[ApiVersion("1.0")]
public class DeleteConversationController : ConversationControllerBase
{
    private readonly IAIConversationService _conversationService;

    /// <summary>Initializes a new instance of the <see cref="DeleteConversationController"/> class.</summary>
    public DeleteConversationController(IAIConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    /// <summary>Deletes one of the acting user's conversations (and purges its files).</summary>
    /// <param name="id">The conversation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content, or 404 if not found for the current user.</returns>
    [HttpDelete("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _conversationService.DeleteConversationAsync(id, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return ConversationNotFound();
        }

        return NoContent();
    }
}

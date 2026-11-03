using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Revokes one recipient's share of one of the acting user's own conversations.
/// </summary>
[ApiVersion("1.0")]
public class RevokeConversationShareController : ConversationControllerBase
{
    private readonly IAIConversationShareService _shareService;

    /// <summary>Initializes a new instance of the <see cref="RevokeConversationShareController"/> class.</summary>
    public RevokeConversationShareController(IAIConversationShareService shareService)
    {
        _shareService = shareService;
    }

    /// <summary>Revokes a recipient's active share of one of the acting user's own conversations.</summary>
    /// <param name="id">The conversation id.</param>
    /// <param name="userKey">The recipient's user key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 204 No Content, or 404 if the conversation isn't owned by the acting user or there is no active
    /// share for that recipient — the two are deliberately indistinguishable.
    /// </returns>
    [HttpDelete("{id:guid}/shares/{userKey:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid id, Guid userKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _shareService.RevokeConversationShareAsync(id, userKey, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return ConversationNotFound();
        }

        return NoContent();
    }
}

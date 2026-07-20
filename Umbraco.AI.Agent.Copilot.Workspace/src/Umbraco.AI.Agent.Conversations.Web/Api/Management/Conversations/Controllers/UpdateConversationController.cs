using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Updates a conversation's metadata (rename, pin, archive, re-home, change agent/profile).
/// </summary>
[ApiVersion("1.0")]
public class UpdateConversationController : ConversationControllerBase
{
    private readonly IAIConversationService _conversationService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="UpdateConversationController"/> class.</summary>
    public UpdateConversationController(IAIConversationService conversationService, IUmbracoMapper umbracoMapper)
    {
        _conversationService = conversationService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Updates one of the acting user's conversations.</summary>
    /// <param name="id">The conversation id.</param>
    /// <param name="model">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content, or 404 if not found for the current user.</returns>
    [HttpPut("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateConversationRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var conversation = _umbracoMapper.Map<AIConversation>(model)!;
        conversation.Id = id;

        try
        {
            await _conversationService.UpdateConversationAsync(conversation, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return ConversationNotFound();
        }

        return NoContent();
    }
}

using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Gets a page of a conversation's messages in sequence order.
/// </summary>
[ApiVersion("1.0")]
public class ConversationMessagesController : ConversationControllerBase
{
    private readonly IAIConversationService _conversationService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="ConversationMessagesController"/> class.</summary>
    public ConversationMessagesController(IAIConversationService conversationService, IUmbracoMapper umbracoMapper)
    {
        _conversationService = conversationService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Gets a page of messages for one of the acting user's conversations.</summary>
    /// <param name="id">The conversation id.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of messages, or 404 if the conversation is not found for the current user.</returns>
    [HttpGet("{id:guid}/messages")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<MessageResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        Guid id,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (items, total) = await _conversationService.GetMessagesPagedAsync(id, skip, take, cancellationToken);

            return Ok(new PagedViewModel<MessageResponseModel>
            {
                Total = total,
                Items = _umbracoMapper.MapEnumerable<AIMessage, MessageResponseModel>(items),
            });
        }
        catch (InvalidOperationException)
        {
            return ConversationNotFound();
        }
    }
}

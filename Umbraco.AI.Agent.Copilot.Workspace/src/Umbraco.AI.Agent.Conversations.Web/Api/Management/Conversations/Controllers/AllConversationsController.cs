using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Lists the acting user's conversations (paged).
/// </summary>
[ApiVersion("1.0")]
public class AllConversationsController : ConversationControllerBase
{
    private readonly IAIConversationService _conversationService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="AllConversationsController"/> class.</summary>
    public AllConversationsController(IAIConversationService conversationService, IUmbracoMapper umbracoMapper)
    {
        _conversationService = conversationService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Gets a paged list of the acting user's conversations, newest activity first.</summary>
    /// <param name="projectId">Optional project filter.</param>
    /// <param name="search">Optional search over title and message content.</param>
    /// <param name="includeArchived">Whether to include archived conversations.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of conversations.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<ConversationResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<ConversationResponseModel>>> GetAll(
        Guid? projectId = null,
        string? search = null,
        bool includeArchived = false,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _conversationService.GetConversationsPagedAsync(
            skip, take, projectId, search, includeArchived, cancellationToken);

        return Ok(new PagedViewModel<ConversationResponseModel>
        {
            Total = total,
            Items = _umbracoMapper.MapEnumerable<AIConversation, ConversationResponseModel>(items),
        });
    }
}

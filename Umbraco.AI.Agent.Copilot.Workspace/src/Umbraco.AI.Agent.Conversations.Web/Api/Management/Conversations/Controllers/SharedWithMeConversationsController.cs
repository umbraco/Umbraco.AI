using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Lists conversations actively shared with the acting user — the recipient's own "shared with me"
/// list. Parallel to <see cref="AllConversationsController"/>'s owned-conversations list, but sourced
/// from <see cref="IAIConversationShareReadService"/> instead: there is no ownership check to apply,
/// since every result is, by definition, a conversation shared with the acting user.
/// </summary>
[ApiVersion("1.0")]
public class SharedWithMeConversationsController : ConversationControllerBase
{
    private readonly IAIConversationShareReadService _shareReadService;
    private readonly IUserService _userService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="SharedWithMeConversationsController"/> class.</summary>
    public SharedWithMeConversationsController(
        IAIConversationShareReadService shareReadService,
        IUserService userService,
        IUmbracoMapper umbracoMapper)
    {
        _shareReadService = shareReadService;
        _userService = userService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets a paged list of conversations actively shared with the acting user.
    /// </summary>
    /// <param name="search">
    /// Optional search, matched against the conversation title only — not message content, unlike
    /// <see cref="AllConversationsController.GetAll"/>'s own search. A Snapshot share hides messages
    /// after its cutoff from the recipient; matching message content here would let a search hit (or
    /// miss) leak whether a word exists past that cutoff.
    /// </param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of shared conversation summaries.</returns>
    [HttpGet("shared-with-me")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<SharedConversationSummaryResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<SharedConversationSummaryResponseModel>>> GetSharedWithMe(
        string? search = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _shareReadService.GetSharedConversationsPagedAsync(skip, take, search, cancellationToken);

        return Ok(new PagedViewModel<SharedConversationSummaryResponseModel>
        {
            Total = total,
            Items = await SharedConversationResponseModelFactory.MapSummariesAsync(_umbracoMapper, _userService, items),
        });
    }
}

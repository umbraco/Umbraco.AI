using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Lists the active (non-revoked) recipients of one of the acting user's own conversations.
/// </summary>
[ApiVersion("1.0")]
public class GetConversationSharesController : ConversationControllerBase
{
    private readonly IAIConversationShareService _shareService;
    private readonly IUserService _userService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="GetConversationSharesController"/> class.</summary>
    public GetConversationSharesController(
        IAIConversationShareService shareService,
        IUserService userService,
        IUmbracoMapper umbracoMapper)
    {
        _shareService = shareService;
        _userService = userService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Gets the active recipients of one of the acting user's own conversations.</summary>
    /// <param name="id">The conversation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with the active shares, or 404 if not found for the current user.</returns>
    [HttpGet("{id:guid}/shares")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ConversationShareResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShares(Guid id, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AIConversationShare> shares;
        try
        {
            shares = await _shareService.GetConversationSharesAsync(id, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return ConversationNotFound();
        }

        return Ok(await ConversationShareResponseModelFactory.MapAsync(_umbracoMapper, _userService, shares));
    }
}

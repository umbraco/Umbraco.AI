using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Gets a conversation shared with the acting user, including its visible messages. The only
/// read surface a non-owner has — there is no "shared" variant of send-message, rename, delete,
/// truncate, or attach-resource.
/// </summary>
[ApiVersion("1.0")]
public class SharedViewConversationController : ConversationControllerBase
{
    private readonly IAIConversationShareReadService _shareReadService;
    private readonly IUserService _userService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="SharedViewConversationController"/> class.</summary>
    public SharedViewConversationController(
        IAIConversationShareReadService shareReadService,
        IUserService userService,
        IUmbracoMapper umbracoMapper)
    {
        _shareReadService = shareReadService;
        _userService = userService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Gets a conversation shared with the acting user, and its visible messages.</summary>
    /// <param name="id">The conversation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 with the shared conversation and its visible messages, or 404 if there is no active share
    /// for <paramref name="id"/> addressed to the acting user — indistinguishable from "doesn't
    /// exist", the same posture as every other not-found case in this API.
    /// </returns>
    [HttpGet("{id:guid}/shared-view")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SharedConversationResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSharedView(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _shareReadService.GetSharedConversationAsync(id, cancellationToken);
        if (result is null)
        {
            return ConversationNotFound();
        }

        var (conversation, messages, scopeMode) = result.Value;
        return Ok(await SharedConversationResponseModelFactory.MapAsync(_umbracoMapper, _userService, conversation, messages, scopeMode));
    }
}

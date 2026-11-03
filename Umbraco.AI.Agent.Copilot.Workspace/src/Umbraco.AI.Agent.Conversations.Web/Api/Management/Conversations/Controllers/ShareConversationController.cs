using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Creates or updates shares of one of the acting user's own conversations with other backoffice users.
/// </summary>
[ApiVersion("1.0")]
public class ShareConversationController : ConversationControllerBase
{
    private readonly IAIConversationShareService _shareService;
    private readonly IUserService _userService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="ShareConversationController"/> class.</summary>
    public ShareConversationController(
        IAIConversationShareService shareService,
        IUserService userService,
        IUmbracoMapper umbracoMapper)
    {
        _shareService = shareService;
        _userService = userService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Shares one of the acting user's conversations with one or more recipients — idempotent per
    /// recipient (an already-shared recipient has their scope mode updated rather than getting a
    /// duplicate row). A recipient a new share row is created for is emailed; updating an existing
    /// recipient's scope does not send a second email.
    /// </summary>
    /// <param name="id">The conversation id.</param>
    /// <param name="model">The share request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 with the resulting active shares, 400 if the scope mode is unrecognized or a Snapshot
    /// cutoff couldn't be resolved, or 404 if not found for the current user.
    /// </returns>
    [HttpPost("{id:guid}/shares")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ConversationShareResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Share(
        Guid id,
        [FromBody] ShareConversationRequestModel model,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AIConversationShareScopeMode>(model.ScopeMode, true, out var scopeMode) || !Enum.IsDefined(scopeMode))
        {
            return ConversationShareScopeModeInvalid(model.ScopeMode);
        }

        IReadOnlyList<AIConversationShare> shares;
        try
        {
            shares = await _shareService.ShareConversationAsync(
                id, model.RecipientUserKeys.ToList(), scopeMode, cancellationToken);
        }
        catch (AIConversationShareCutoffUnresolvableException ex)
        {
            // Must be caught ahead of the InvalidOperationException below — this type derives from it,
            // and the two mean very different things to the caller (a rejected request vs. a 404).
            return ConversationShareCutoffUnresolvable(ex);
        }
        catch (InvalidOperationException)
        {
            return ConversationNotFound();
        }

        return Ok(await ConversationShareResponseModelFactory.MapAsync(_umbracoMapper, _userService, shares));
    }
}

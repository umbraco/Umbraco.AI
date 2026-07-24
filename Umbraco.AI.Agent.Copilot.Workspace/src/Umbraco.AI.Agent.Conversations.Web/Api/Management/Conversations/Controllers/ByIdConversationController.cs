using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Gets a single conversation by id.
/// </summary>
[ApiVersion("1.0")]
public class ByIdConversationController : ConversationControllerBase
{
    private readonly IAIConversationService _conversationService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="ByIdConversationController"/> class.</summary>
    public ByIdConversationController(IAIConversationService conversationService, IUmbracoMapper umbracoMapper)
    {
        _conversationService = conversationService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Gets one of the acting user's conversations by id.</summary>
    /// <param name="id">The conversation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversation, or 404 if not found for the current user.</returns>
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ConversationResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationService.GetConversationAsync(id, cancellationToken);
        if (conversation is null)
        {
            return ConversationNotFound();
        }

        return Ok(_umbracoMapper.Map<ConversationResponseModel>(conversation));
    }
}

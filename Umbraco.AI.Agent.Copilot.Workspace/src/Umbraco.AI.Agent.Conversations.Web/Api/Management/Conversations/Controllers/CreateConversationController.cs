using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Creates a conversation.
/// </summary>
[ApiVersion("1.0")]
public class CreateConversationController : ConversationControllerBase
{
    private readonly IAIConversationService _conversationService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="CreateConversationController"/> class.</summary>
    public CreateConversationController(IAIConversationService conversationService, IUmbracoMapper umbracoMapper)
    {
        _conversationService = conversationService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Creates a conversation owned by the acting user.</summary>
    /// <param name="model">The creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created conversation's id.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ConversationResponseModel), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateConversationRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var conversation = _umbracoMapper.Map<AIConversation>(model)!;
        var created = await _conversationService.CreateConversationAsync(conversation, cancellationToken);

        return CreatedAtAction(
            nameof(ByIdConversationController.GetById),
            "ByIdConversation",
            new { id = created.Id },
            _umbracoMapper.Map<ConversationResponseModel>(created));
    }
}

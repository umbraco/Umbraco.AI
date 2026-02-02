using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Chat.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Chat.Controllers;

/// <summary>
/// Controller for non-streaming chat completion.
/// </summary>
[ApiVersion("1.0")]
public class CompleteChatController : ChatControllerBase
{
    private readonly IAiChatService _chatService;
    private readonly IAiProfileService _profileService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteChatController"/> class.
    /// </summary>
    public CompleteChatController(
        IAiChatService chatService,
        IAiProfileService profileService,
        IUmbracoMapper umbracoMapper)
    {
        _chatService = chatService;
        _profileService = profileService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Complete a chat conversation (non-streaming).
    /// </summary>
    /// <param name="profileIdOrAlias"></param>
    /// <param name="requestModel">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat completion response.</returns>
    [HttpPost("complete")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ChatResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteChat(
        [FromHeader] IdOrAlias? profileIdOrAlias,
        ChatRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Resolve the profile ID
            var profileId = profileIdOrAlias != null
                ? await _profileService.TryGetProfileIdAsync(profileIdOrAlias, cancellationToken)
                : null;
                
            // Convert request messages to ChatMessage list
            var messages = _umbracoMapper.MapEnumerable<ChatMessageModel, ChatMessage>(requestModel.Messages).ToList();

            // Get chat response
            var response = profileId.HasValue
                ? await _chatService.GetChatResponseAsync(
                    profileId.Value,
                    messages,
                    cancellationToken: cancellationToken)
                : await _chatService.GetChatResponseAsync(
                    messages,
                    cancellationToken: cancellationToken);

            return Ok(_umbracoMapper.Map<ChatResponseModel>(response));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Chat completion failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}

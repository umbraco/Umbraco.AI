using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Services;
using Umbraco.Ai.Web.Api.Management.Chat.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Chat.Controllers;

/// <summary>
/// Controller for non-streaming chat completion.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CompleteChatController : ChatControllerBase
{
    private readonly IAiChatService _chatService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteChatController"/> class.
    /// </summary>
    public CompleteChatController(IAiChatService chatService, IUmbracoMapper umbracoMapper)
    {
        _chatService = chatService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Complete a chat conversation (non-streaming).
    /// </summary>
    /// <param name="requestModel">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat completion response.</returns>
    [HttpPost("complete")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ChatResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(
        ChatRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert request messages to ChatMessage list
            var messages = _umbracoMapper.MapEnumerable<ChatMessageModel, ChatMessage>(requestModel.Messages).ToList();

            // Get chat response
            var response = requestModel.ProfileId.HasValue
                ? await _chatService.GetResponseAsync(
                    requestModel.ProfileId.Value,
                    messages,
                    cancellationToken: cancellationToken)
                : await _chatService.GetResponseAsync(
                    messages,
                    cancellationToken: cancellationToken);

            return Ok(_umbracoMapper.Map<ChatResponseModel>(response));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return ProfileNotFound();
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

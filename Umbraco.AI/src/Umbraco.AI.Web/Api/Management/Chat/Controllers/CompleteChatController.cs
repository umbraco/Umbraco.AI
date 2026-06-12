using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.InlineChat;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Chat.Models;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Chat.Controllers;

/// <summary>
/// Controller for non-streaming chat completion.
/// </summary>
[ApiVersion("1.0")]
public class CompleteChatController : ChatControllerBase
{
    private readonly IAIChatService _chatService;
    private readonly IAIProfileService _profileService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteChatController"/> class.
    /// </summary>
    public CompleteChatController(
        IAIChatService chatService,
        IAIProfileService profileService,
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CompleteChat(
        [FromHeader] IdOrAlias? profileIdOrAlias,
        ChatRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Resolve the profile ID
            Guid? profileId = null;
            if (profileIdOrAlias != null)
            {
                profileId = await _profileService.TryGetProfileIdAsync(profileIdOrAlias, cancellationToken);
                if (!profileId.HasValue)
                {
                    return ProfileNotFound();
                }
            }

            // Convert request messages to ChatMessage list
            var messages = _umbracoMapper.MapEnumerable<ChatMessageModel, ChatMessage>(requestModel.Messages).ToList();

            // Get chat response
            var response = await _chatService.GetChatResponseAsync(chat =>
            {
                chat.WithAlias("management-api-chat");
                if (profileId.HasValue)
                {
                    chat.WithProfile(profileId.Value);
                }
            }, messages, cancellationToken);

            return Ok(_umbracoMapper.Map<ChatResponseModel>(response));
        }
        catch (AIGuardrailBlockedException ex)
        {
            // A guardrail policy refused the input or the generated response. This is a
            // deliberate policy outcome, not a server fault, so surface it as a structured
            // 422 (with the phase and flagged rules) instead of letting it become a 500.
            var phase = ex.EvaluationResult.Phase == AIGuardrailPhase.PreGenerate ? "input" : "response";
            var problemDetails = new ProblemDetails
            {
                Title = "Chat blocked by guardrail policy",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity
            };
            problemDetails.Extensions["phase"] = phase;
            problemDetails.Extensions["flaggedRules"] = ex.EvaluationResult.RuleResults
                .Where(r => r.EvaluatorResult.Flagged)
                .Select(r => string.IsNullOrWhiteSpace(r.Rule.GuardrailName)
                    ? r.Rule.Name
                    : $"{r.Rule.GuardrailName} > {r.Rule.Name}")
                .ToList();
            return UnprocessableEntity(problemDetails);
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

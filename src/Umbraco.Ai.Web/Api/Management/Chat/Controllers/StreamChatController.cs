using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Chat.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Chat.Controllers;

/// <summary>
/// Controller for streaming chat completion using Server-Sent Events (SSE).
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class StreamChatController : ChatControllerBase
{
    private readonly IAiChatService _chatService;
    private readonly IAiProfileRepository _profileRepository;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamChatController"/> class.
    /// </summary>
    public StreamChatController(
        IAiChatService chatService,
        IAiProfileRepository profileRepository,
        IUmbracoMapper umbracoMapper)
    {
        _chatService = chatService;
        _profileRepository = profileRepository;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Complete a chat conversation with streaming response (SSE).
    /// </summary>
    /// <param name="requestModel">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of chat completion chunks.</returns>
    [HttpPost("stream")]
    [MapToApiVersion("1.0")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task StreamChat(
        ChatRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            // Convert request messages to ChatMessage list
            var messages = _umbracoMapper.MapEnumerable<ChatMessageModel, ChatMessage>(requestModel.Messages).ToList();

            // Resolve profile ID from IdOrAlias
            var profileId = requestModel.ProfileIdOrAlias != null
                ? await _profileRepository.TryGetProfileIdAsync(requestModel.ProfileIdOrAlias, cancellationToken)
                : null;

            // Get streaming chat response
            var stream = profileId.HasValue
                ? _chatService.GetStreamingResponseAsync(
                    profileId.Value,
                    messages,
                    cancellationToken: cancellationToken)
                : _chatService.GetStreamingResponseAsync(
                    messages,
                    cancellationToken: cancellationToken);

            await foreach (var update in stream.WithCancellation(cancellationToken))
            {
                var chunk = _umbracoMapper.Map<ChatStreamChunkModel>(update);

                var json = JsonSerializer.Serialize(chunk);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            // Send final event
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            var error = JsonSerializer.Serialize(new { error = "Profile not found", detail = ex.Message });
            await Response.WriteAsync($"data: {error}\n\n", cancellationToken);
        }
        catch (Exception ex)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            var error = JsonSerializer.Serialize(new { error = "Chat streaming failed", detail = ex.Message });
            await Response.WriteAsync($"data: {error}\n\n", cancellationToken);
        }
    }
}

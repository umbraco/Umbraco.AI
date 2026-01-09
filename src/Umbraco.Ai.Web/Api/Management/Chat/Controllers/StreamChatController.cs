using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Events.Lifecycle;
using Umbraco.Ai.Agui.Events.Messages;
using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Agui.Streaming;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Chat.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;

namespace Umbraco.Ai.Web.Api.Management.Chat.Controllers;

/// <summary>
/// Controller for streaming chat completion using AG-UI protocol over Server-Sent Events (SSE).
/// </summary>
[ApiVersion("1.0")]
public class StreamChatController : ChatControllerBase
{
    private readonly IAiChatService _chatService;
    private readonly IAiProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamChatController"/> class.
    /// </summary>
    public StreamChatController(
        IAiChatService chatService,
        IAiProfileService profileService)
    {
        _chatService = chatService;
        _profileService = profileService;
    }

    /// <summary>
    /// Complete a chat conversation with AG-UI streaming response (SSE).
    /// </summary>
    /// <param name="profileIdOrAlias"></param>
    /// <param name="request">The AG-UI run request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of AG-UI events.</returns>
    [HttpPost("stream")]
    [MapToApiVersion("1.0")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> StreamChat(
        [FromHeader] IdOrAlias?  profileIdOrAlias,
        AguiRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = profileIdOrAlias != null
            ? await _profileService.TryGetProfileIdAsync(profileIdOrAlias, cancellationToken)
            : null;
        
        var events = StreamAguiEventsAsync(request, profileId, cancellationToken);
        
        return new AguiEventStreamResult(events);
    }

    private async IAsyncEnumerable<IAguiEvent> StreamAguiEventsAsync(
        AguiRunRequest request,
        Guid? profileId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var threadId = string.IsNullOrEmpty(request.ThreadId) ? Guid.NewGuid().ToString() : request.ThreadId;
        var runId = string.IsNullOrEmpty(request.RunId) ? Guid.NewGuid().ToString() : request.RunId;
        var messageId = Guid.NewGuid().ToString();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Emit RunStarted
        yield return new RunStartedEvent
        {
            ThreadId = threadId,
            RunId = runId,
            Timestamp = timestamp
        };

        // Emit TextMessageStart for the assistant response
        yield return new TextMessageStartEvent
        {
            MessageId = messageId,
            Role = AguiMessageRole.Assistant,
            Timestamp = timestamp
        };

        // Use Channel for streaming with proper error handling
        var channel = Channel.CreateUnbounded<IAguiEvent>();
        var hasError = false;
        string? errorMessage = null;

        // Start background task to produce events
        _ = Task.Run(async () =>
        {
            try
            {
                // Convert AG-UI messages to M.E.AI ChatMessages
                var chatMessages = ConvertToChatMessages(request.Messages);

                // Get streaming response from chat service
                var stream = profileId.HasValue
                    ? _chatService.GetStreamingChatResponseAsync(profileId.Value, chatMessages, cancellationToken: cancellationToken)
                    : _chatService.GetStreamingChatResponseAsync(chatMessages, cancellationToken: cancellationToken);

                // Stream text content updates
                await foreach (var update in stream.WithCancellation(cancellationToken))
                {
                    var text = update.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        await channel.Writer.WriteAsync(new TextMessageContentEvent
                        {
                            MessageId = messageId,
                            Delta = text,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        }, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                hasError = true;
                errorMessage = ex.Message;
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        // Read from channel and yield events
        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }

        // Emit TextMessageEnd
        yield return new TextMessageEndEvent
        {
            MessageId = messageId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        if (hasError)
        {
            // Emit error event
            yield return new RunErrorEvent
            {
                Message = errorMessage ?? "Unknown error occurred",
                Code = "CHAT_ERROR",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        // Emit RunFinished
        yield return new RunFinishedEvent
        {
            ThreadId = threadId,
            RunId = runId,
            Outcome = hasError ? AguiRunOutcome.Interrupt : AguiRunOutcome.Success,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    private static List<ChatMessage> ConvertToChatMessages(IEnumerable<AguiMessage> messages)
    {
        var chatMessages = new List<ChatMessage>();

        foreach (var msg in messages)
        {
            var role = msg.Role switch
            {
                AguiMessageRole.User => ChatRole.User,
                AguiMessageRole.Assistant => ChatRole.Assistant,
                AguiMessageRole.System => ChatRole.System,
                AguiMessageRole.Tool => ChatRole.Tool,
                AguiMessageRole.Developer => ChatRole.System, // Map developer to system
                _ => ChatRole.User
            };

            chatMessages.Add(new ChatMessage(role, msg.Content ?? string.Empty));
        }

        return chatMessages;
    }
}

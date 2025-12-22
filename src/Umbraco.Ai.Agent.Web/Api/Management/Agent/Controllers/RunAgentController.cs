using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Extensions;
using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Events.Lifecycle;
using Umbraco.Ai.Agui.Events.Messages;
using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Agui.Streaming;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for running agents with AG-UI streaming support.
/// </summary>
[ApiVersion("1.0")]
public class RunAgentController : AgentControllerBase
{
    private readonly IAiAgentService _agentService;
    private readonly IAiProfileService _profileService;
    private readonly IAiChatClientFactory _chatClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAgentController"/> class.
    /// </summary>
    public RunAgentController(
        IAiAgentService agentService,
        IAiProfileService profileService,
        IAiChatClientFactory chatClientFactory)
    {
        _agentService = agentService;
        _profileService = profileService;
        _chatClientFactory = chatClientFactory;
    }

    /// <summary>
    /// Runs an agent with AG-UI streaming response (SSE).
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias.</param>
    /// <param name="request">The AG-UI run request containing messages and context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of AG-UI events.</returns>
    [HttpPost($"{{{nameof(agentIdOrAlias)}}}/run")]
    [MapToApiVersion("1.0")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> RunAgent(
        IdOrAlias agentIdOrAlias,
        AguiRunRequest request,
        CancellationToken cancellationToken = default)
    {
        // Resolve the agent
        var agent = await _agentService.GetAgentAsync(agentIdOrAlias, cancellationToken);
        if (agent is null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "AiAgent not found",
                Detail = "The specified agent could not be found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (!agent.IsActive)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Agent not active",
                Detail = $"Agent '{agent.Name}' is not active.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Get the agent's profile
        var profile = await _profileService.GetProfileAsync(agent.ProfileId, cancellationToken);
        if (profile is null)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Profile not found",
                Detail = $"The profile configured for agent '{agent.Name}' could not be found.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Stream the response
        var events = StreamAgentEventsAsync(agent, profile, request, cancellationToken);
        
        return new AguiEventStreamResult(events);
    }

    private async IAsyncEnumerable<IAguiEvent> StreamAgentEventsAsync(
        AiAgent agent,
        AiProfile profile,
        AguiRunRequest request,
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
                // Create chat client from profile
                var chatClient = await _chatClientFactory.CreateClientAsync(profile, cancellationToken);

                // Convert AG-UI messages to M.E.AI ChatMessages
                var chatMessages = ConvertToChatMessages(agent, request.Messages);

                // Get streaming response
                await foreach (var update in chatClient.GetStreamingResponseAsync(chatMessages, cancellationToken: cancellationToken))
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
                Code = "AGENT_RUN_ERROR",
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

    private static List<ChatMessage> ConvertToChatMessages(AiAgent agent, IEnumerable<AguiMessage> messages)
    {
        var chatMessages = new List<ChatMessage>();

        // Add agent instructions as system message if present
        if (!string.IsNullOrWhiteSpace(agent.Instructions))
        {
            chatMessages.Add(new ChatMessage(ChatRole.System, agent.Instructions));
        }

        // Convert AG-UI messages
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

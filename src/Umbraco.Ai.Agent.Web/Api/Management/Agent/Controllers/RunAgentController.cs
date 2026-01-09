using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.Agent.Extensions;
using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Events.Lifecycle;
using Umbraco.Ai.Agui.Events.Messages;
using Umbraco.Ai.Agui.Events.Tools;
using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Agui.Streaming;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for running agents with AG-UI streaming support.
/// </summary>
/// <remarks>
/// <para>
/// This controller uses the Microsoft Agent Framework (MAF) for agent execution,
/// but maintains a custom implementation rather than using MAF's built-in <c>MapAGUI()</c>
/// for the following reasons:
/// </para>
/// <list type="bullet">
///   <item>Frontend tool handling with <c>FunctionInvokingChatClient.CurrentContext.Terminate</c></item>
///   <item>Umbraco authorization/security model integration</item>
///   <item>Custom AG-UI context item handling</item>
/// </list>
/// </remarks>
[ApiVersion("1.0")]
public class RunAgentController : AgentControllerBase
{
    private readonly IAiAgentService _agentService;
    private readonly IAiProfileService _profileService;
    private readonly IAiAgentFactory _agentFactory;
    private readonly ILogger<RunAgentController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAgentController"/> class.
    /// </summary>
    public RunAgentController(
        IAiAgentService agentService,
        IAiProfileService profileService,
        IAiAgentFactory agentFactory,
        ILogger<RunAgentController> logger)
    {
        _agentService = agentService;
        _profileService = profileService;
        _agentFactory = agentFactory;
        _logger = logger;
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

        // Get the agent's profile (for validation only - factory handles profile binding)
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
        var events = StreamAgentEventsAsync(agent, request, cancellationToken);

        return new AguiEventStreamResult(events);
    }

    private async IAsyncEnumerable<IAguiEvent> StreamAgentEventsAsync(
        AiAgent agent,
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

        // NOTE: No TextMessageStartEvent here - we use CHUNK events instead.
        // The frontend transformChunks operator will convert CHUNK → START/CONTENT/END.

        // Use a channel to bridge between try-catch and yield
        var channel = Channel.CreateUnbounded<IAguiEvent>();

        // Build frontend tool names set for quick lookup
        var frontendToolNames = new HashSet<string>(
            request.Tools?.Select(t => t.Name) ?? [],
            StringComparer.OrdinalIgnoreCase);

        var streamingTask = Task.Run(async () =>
        {
            var hasError = false;
            string? errorMessage = null;
            var emittedToolCallIds = new HashSet<string>();
            // Track frontend tool call IDs (for skipping their fake results)
            var frontendToolCallIds = new HashSet<string>();
            // Track current messageId - will be regenerated after each tool result for multi-block UI
            var currentMessageId = messageId;

            try
            {
                // Convert AG-UI frontend tools to AITool format (FrontendToolFunction)
                var frontendTools = request.Tools?.Any() == true
                    ? ConvertToFrontendTools(request.Tools)
                    : null;

                // Create MAF agent with all tools (system + user + frontend)
                var mafAgent = await _agentFactory.CreateAgentAsync(agent, frontendTools, cancellationToken);

                // Convert AG-UI messages to M.E.AI ChatMessages (context items included)
                // Note: Agent instructions are handled by AgentBoundChatClient, not here
                var chatMessages = ConvertToChatMessages(request.Messages, request.Context);

                // DEBUG: Log that we're using MAF streaming
                _logger.LogDebug("Starting MAF agent streaming for agent '{AgentName}' ({AgentId})",
                    agent.Name, agent.Id);

                // Use MAF streaming - AgentRunResponseUpdate has same Contents structure as ChatResponseUpdate
                await foreach (var update in mafAgent.RunStreamingAsync(chatMessages, cancellationToken: cancellationToken))
                {
                    // Check for tool calls and results FIRST (before text)
                    // This ensures tool call UI appears before any follow-up text
                    if (update.Contents != null)
                    {
                        foreach (var content in update.Contents)
                        {
                            if (content is FunctionCallContent functionCall &&
                                !string.IsNullOrEmpty(functionCall.CallId) &&
                                !emittedToolCallIds.Contains(functionCall.CallId))
                            {
                                emittedToolCallIds.Add(functionCall.CallId);

                                // Track if this is a frontend tool
                                var isFrontendTool = frontendToolNames.Contains(functionCall.Name);
                                if (isFrontendTool)
                                {
                                    frontendToolCallIds.Add(functionCall.CallId);
                                }

                                // Emit TOOL_CALL_CHUNK immediately with name and args
                                var argsJson = functionCall.Arguments != null
                                    ? JsonSerializer.Serialize(functionCall.Arguments)
                                    : "{}";

                                await channel.Writer.WriteAsync(new ToolCallChunkEvent
                                {
                                    ToolCallId = functionCall.CallId,
                                    ToolCallName = functionCall.Name,
                                    ParentMessageId = currentMessageId,
                                    Delta = argsJson,
                                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                }, cancellationToken);
                            }
                            else if (content is FunctionResultContent functionResult &&
                                     !string.IsNullOrEmpty(functionResult.CallId))
                            {
                                // Skip emitting results for frontend tools - they execute on client
                                if (frontendToolCallIds.Contains(functionResult.CallId))
                                {
                                    continue;
                                }

                                // Emit TOOL_CALL_RESULT for backend tools only
                                var resultJson = functionResult.Result != null
                                    ? JsonSerializer.Serialize(functionResult.Result)
                                    : "null";

                                await channel.Writer.WriteAsync(new ToolCallResultEvent
                                {
                                    MessageId = Guid.NewGuid().ToString(),
                                    ToolCallId = functionResult.CallId,
                                    Content = resultJson,
                                    Role = AguiMessageRole.Tool,
                                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                }, cancellationToken);

                                // Generate new messageId for any following text (multi-block UI)
                                currentMessageId = Guid.NewGuid().ToString();
                            }
                        }
                    }

                    // Handle text content - emit as TEXT_MESSAGE_CHUNK
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        await channel.Writer.WriteAsync(new TextMessageChunkEvent
                        {
                            MessageId = currentMessageId,
                            Role = AguiMessageRole.Assistant,
                            Delta = update.Text,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        }, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                hasError = true;
                errorMessage = ex.Message;
                _logger.LogError(ex, "Error streaming MAF agent response for agent '{AgentName}'", agent.Name);
            }

            // NOTE: No TextMessageEndEvent or batched tool events here.
            // The frontend transformChunks operator handles closing messages on mode switch.

            if (hasError)
            {
                await channel.Writer.WriteAsync(new RunErrorEvent
                {
                    Message = errorMessage ?? "Unknown error occurred",
                    Code = "AGENT_RUN_ERROR",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }, CancellationToken.None);
            }

            // Determine run outcome and interrupt info
            // Use tracked frontend tool call IDs instead of AsyncLocal (more reliable across Task.Run boundaries)
            var hasFrontendTools = frontendToolCallIds.Count > 0;
            var outcome = hasError
                ? AguiRunOutcome.Error
                : hasFrontendTools
                    ? AguiRunOutcome.Interrupt
                    : AguiRunOutcome.Success;

            // Build interrupt info if frontend tools need execution
            AguiInterruptInfo? interrupt = hasFrontendTools
                ? new AguiInterruptInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Reason = "tool_execution",
                }
                : null;

            // Emit RunFinished
            await channel.Writer.WriteAsync(new RunFinishedEvent
            {
                ThreadId = threadId,
                RunId = runId,
                Outcome = outcome,
                Interrupt = interrupt,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, CancellationToken.None);

            channel.Writer.Complete();
        }, cancellationToken);

        // Yield events from the channel
        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }

        await streamingTask;
    }

    /// <summary>
    /// Convert AG-UI tools to M.E.AI AITool format as frontend tools.
    /// These tools expose their schema to the LLM but execution happens on the client.
    /// </summary>
    private static IList<AITool> ConvertToFrontendTools(IEnumerable<AguiTool> tools)
    {
        var aiTools = new List<AITool>();

        foreach (var tool in tools)
        {
            aiTools.Add(new FrontendToolFunction(tool));
        }

        return aiTools;
    }

    /// <summary>
    /// A frontend tool function that exposes the tool's parameter schema to the LLM
    /// but doesn't actually execute - execution happens on the client.
    /// </summary>
    private sealed class FrontendToolFunction : AIFunction
    {
        private readonly string _name;
        private readonly string _description;
        private readonly JsonElement _jsonSchema;

        public FrontendToolFunction(AguiTool tool)
        {
            _name = tool.Name;
            _description = tool.Description;
            _jsonSchema = BuildJsonSchema(tool.Parameters);
        }

        private static JsonElement BuildJsonSchema(AguiToolParameters parameters)
        {
            var schemaObj = new Dictionary<string, object?>
            {
                ["type"] = parameters.Type,
                ["properties"] = parameters.Properties,
            };

            if (parameters.Required?.Any() == true)
            {
                schemaObj["required"] = parameters.Required;
            }

            return JsonSerializer.SerializeToElement(schemaObj);
        }

        public override string Name => _name;

        public override string Description => _description;

        public override JsonElement JsonSchema => _jsonSchema;

        protected override ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            // Tell FunctionInvokingChatClient to stop its invocation loop.
            // This allows us to return out and emit the tool call to the frontend
            // instead of auto-executing and feeding a fake result back to the model.
            if (FunctionInvokingChatClient.CurrentContext is not null)
            {
                FunctionInvokingChatClient.CurrentContext.Terminate = true;
            }

            return ValueTask.FromResult<object?>(null);
        }
    }

    /// <summary>
    /// Convert AG-UI messages to M.E.AI ChatMessages.
    /// </summary>
    /// <remarks>
    /// Agent instructions are handled by <see cref="AgentBoundChatClient"/>, not here.
    /// This method only handles AG-UI context items and message conversion.
    /// </remarks>
    private static List<ChatMessage> ConvertToChatMessages(
        IEnumerable<AguiMessage> messages,
        IEnumerable<AguiContextItem>? context)
    {
        var chatMessages = new List<ChatMessage>();

        // Build system message with context items only (agent instructions handled by AgentBoundChatClient)
        if (context?.Any() == true)
        {
            var systemContent = new StringBuilder();
            systemContent.AppendLine("## Current Context");
            foreach (var item in context)
            {
                systemContent.AppendLine($"### {item.Description}");
                if (item.Value.HasValue)
                {
                    systemContent.AppendLine("```json");
                    systemContent.AppendLine(item.Value.Value.GetRawText());
                    systemContent.AppendLine("```");
                }
            }

            chatMessages.Add(new ChatMessage(ChatRole.System, systemContent.ToString()));
        }

        // Convert AG-UI messages
        foreach (var msg in messages)
        {
            if (msg.Role == AguiMessageRole.Assistant && msg.ToolCalls?.Any() == true)
            {
                // Assistant message with tool calls - include FunctionCallContent
                var contents = new List<AIContent>();

                // Add text content if present
                if (!string.IsNullOrEmpty(msg.Content))
                {
                    contents.Add(new TextContent(msg.Content));
                }

                // Add function call content for each tool call
                foreach (var toolCall in msg.ToolCalls)
                {
                    // Parse arguments from JSON string to dictionary
                    IDictionary<string, object?>? args = null;
                    if (!string.IsNullOrEmpty(toolCall.Function.Arguments))
                    {
                        try
                        {
                            args = JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.Function.Arguments);
                        }
                        catch
                        {
                            // If parsing fails, use empty dict
                            args = new Dictionary<string, object?>();
                        }
                    }

                    contents.Add(new FunctionCallContent(toolCall.Id, toolCall.Function.Name, args));
                }

                chatMessages.Add(new ChatMessage(ChatRole.Assistant, contents));
            }
            else if (msg.Role == AguiMessageRole.Tool && !string.IsNullOrEmpty(msg.ToolCallId))
            {
                // Tool result message - include FunctionResultContent
                var result = new FunctionResultContent(msg.ToolCallId, msg.Content ?? string.Empty);
                chatMessages.Add(new ChatMessage(ChatRole.Tool, [result]));
            }
            else
            {
                // Regular message
                var role = msg.Role switch
                {
                    AguiMessageRole.User => ChatRole.User,
                    AguiMessageRole.Assistant => ChatRole.Assistant,
                    AguiMessageRole.System => ChatRole.System,
                    AguiMessageRole.Tool => ChatRole.Tool,
                    AguiMessageRole.Developer => ChatRole.System,
                    _ => ChatRole.User
                };

                chatMessages.Add(new ChatMessage(role, msg.Content ?? string.Empty));
            }
        }

        return chatMessages;
    }
}

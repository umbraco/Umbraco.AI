using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Extensions;
using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Events.Lifecycle;
using Umbraco.Ai.Agui.Events.Messages;
using Umbraco.Ai.Agui.Events.Tools;
using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Agui.Streaming;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Tools;
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
    private readonly AiToolCollection _toolCollection;
    private readonly IAiFunctionFactory _functionFactory;
    private readonly ILogger<RunAgentController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAgentController"/> class.
    /// </summary>
    public RunAgentController(
        IAiAgentService agentService,
        IAiProfileService profileService,
        IAiChatClientFactory chatClientFactory,
        AiToolCollection toolCollection,
        IAiFunctionFactory functionFactory,
        ILogger<RunAgentController> logger)
    {
        _agentService = agentService;
        _profileService = profileService;
        _chatClientFactory = chatClientFactory;
        _toolCollection = toolCollection;
        _functionFactory = functionFactory;
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

        // Use a channel to bridge between try-catch and yield
        var channel = Channel.CreateUnbounded<IAguiEvent>();

        // Start the streaming task
        var streamingTask = Task.Run(async () =>
        {
            var hasError = false;
            string? errorMessage = null;
            var toolCalls = new List<FunctionCallContent>();
            var emittedToolCallIds = new HashSet<string>();
            var toolResults = new Dictionary<string, string>();

            try
            {
                // Reset frontend tool invocation flag for this run
                FrontendToolFunction.ResetInvocationFlag();

                // Create chat client
                var chatClient = await _chatClientFactory.CreateClientAsync(profile, cancellationToken);

                // Convert AG-UI messages to M.E.AI ChatMessages
                var chatMessages = ConvertToChatMessages(agent, request.Messages);

                // Build ChatOptions with profile settings
                var chatOptions = BuildChatOptions(profile, request.Tools);

                // DEBUG: Log tool schemas being sent to the LLM
                if (chatOptions.Tools != null)
                {
                    foreach (var tool in chatOptions.Tools.OfType<AIFunction>())
                    {
                        _logger.LogDebug(
                            "Tool '{ToolName}' JsonSchema: {Schema}",
                            tool.Name,
                            tool.JsonSchema.GetRawText());
                    }
                }

                // Stream the response
                await foreach (var update in chatClient.GetStreamingResponseAsync(chatMessages, chatOptions, cancellationToken))
                {
                    // Handle text content
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        await channel.Writer.WriteAsync(new TextMessageContentEvent
                        {
                            MessageId = messageId,
                            Delta = update.Text,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        }, cancellationToken);
                    }

                    // Check for tool calls and results in the update contents
                    if (update.Contents != null)
                    {
                        foreach (var content in update.Contents)
                        {
                            if (content is FunctionCallContent functionCall && !string.IsNullOrEmpty(functionCall.CallId))
                            {
                                // Track tool calls for later processing
                                if (!emittedToolCallIds.Contains(functionCall.CallId))
                                {
                                    toolCalls.Add(functionCall);
                                }
                            }
                            else if (content is FunctionResultContent functionResult && !string.IsNullOrEmpty(functionResult.CallId))
                            {
                                // Track tool results for backend tools
                                var resultJson = functionResult.Result != null
                                    ? JsonSerializer.Serialize(functionResult.Result)
                                    : "null";
                                toolResults[functionResult.CallId] = resultJson;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                hasError = true;
                errorMessage = ex.Message;
            }

            // Emit TextMessageEnd (before tool calls)
            await channel.Writer.WriteAsync(new TextMessageEndEvent
            {
                MessageId = messageId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, CancellationToken.None);

            // Emit tool call events for any tool calls detected
            foreach (var toolCall in toolCalls)
            {
                if (emittedToolCallIds.Contains(toolCall.CallId!))
                    continue;

                emittedToolCallIds.Add(toolCall.CallId!);

                // TOOL_CALL_START
                await channel.Writer.WriteAsync(new ToolCallStartEvent
                {
                    ToolCallId = toolCall.CallId!,
                    ToolCallName = toolCall.Name,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }, CancellationToken.None);

                // TOOL_CALL_ARGS - send arguments as JSON
                var argsJson = toolCall.Arguments != null
                    ? JsonSerializer.Serialize(toolCall.Arguments)
                    : "{}";

                await channel.Writer.WriteAsync(new ToolCallArgsEvent
                {
                    ToolCallId = toolCall.CallId!,
                    Delta = argsJson,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }, CancellationToken.None);

                // TOOL_CALL_END
                await channel.Writer.WriteAsync(new ToolCallEndEvent
                {
                    ToolCallId = toolCall.CallId!,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }, CancellationToken.None);

                // TOOL_CALL_RESULT - emit for backend tools that have results
                if (toolResults.TryGetValue(toolCall.CallId!, out var resultContent))
                {
                    await channel.Writer.WriteAsync(new ToolCallResultEvent
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        ToolCallId = toolCall.CallId!,
                        Content = resultContent,
                        Role = AguiMessageRole.Tool,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }, CancellationToken.None);
                }
            }

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
            var frontendToolInvoked = FrontendToolFunction.WasInvoked;
            var outcome = hasError
                ? AguiRunOutcome.Error
                : frontendToolInvoked
                    ? AguiRunOutcome.Interrupt
                    : AguiRunOutcome.Success;

            // Build interrupt info if frontend tools need execution
            AguiInterruptInfo? interrupt = frontendToolInvoked
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
    /// Convert AG-UI tools to M.E.AI AITool format.
    /// These are "frontend tools" - the LLM can call them, but execution happens on the client.
    /// </summary>
    private static IList<AITool> ConvertToAITools(IEnumerable<AguiTool> tools)
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

        /// <summary>
        /// Thread-static flag to track if any frontend tool was invoked during the current run.
        /// Used to determine if the run should finish with an interrupt.
        /// </summary>
        [ThreadStatic]
        private static bool _wasInvoked;

        /// <summary>
        /// Gets whether any frontend tool was invoked during the current run.
        /// </summary>
        public static bool WasInvoked => _wasInvoked;

        /// <summary>
        /// Resets the invocation flag. Call this at the start of each run.
        /// </summary>
        public static void ResetInvocationFlag() => _wasInvoked = false;

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
            // Mark that a frontend tool was invoked
            _wasInvoked = true;

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
    /// Build ChatOptions with profile settings applied.
    /// </summary>
    private ChatOptions BuildChatOptions(AiProfile profile, IEnumerable<AguiTool>? frontendTools)
    {
        var chatOptions = new ChatOptions();

        // Apply profile settings (Temperature, MaxTokens) if available
        if (profile.Settings is AiChatProfileSettings chatSettings)
        {
            if (chatSettings.Temperature.HasValue)
            {
                chatOptions.Temperature = chatSettings.Temperature.Value;
            }

            if (chatSettings.MaxTokens.HasValue)
            {
                chatOptions.MaxOutputTokens = chatSettings.MaxTokens.Value;
            }
        }

        // Build combined tool list (backend + frontend)
        var allTools = new List<AITool>();

        // Add backend tools - these execute server-side automatically
        if (_toolCollection.Any())
        {
            var backendFunctions = _functionFactory.Create(_toolCollection);
            allTools.AddRange(backendFunctions);
        }

        // Add frontend tools - these return to client for execution
        if (frontendTools?.Any() == true)
        {
            allTools.AddRange(ConvertToAITools(frontendTools));
        }

        // Configure tools if any exist
        if (allTools.Count > 0)
        {
            chatOptions.Tools = allTools;
            chatOptions.ToolMode = ChatToolMode.Auto;
            // Process one tool call at a time for more predictable behavior
            chatOptions.AllowMultipleToolCalls = false;
        }

        return chatOptions;
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

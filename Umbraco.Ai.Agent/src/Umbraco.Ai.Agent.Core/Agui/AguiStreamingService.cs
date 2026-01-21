using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Events.Lifecycle;
using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Agui.Streaming;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Agent.Core.Agui;

/// <summary>
/// Default implementation of <see cref="IAguiStreamingService"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses direct streaming without <c>Task.Run()</c>, which preserves
/// AsyncLocal context including <see cref="FunctionInvokingChatClient.CurrentContext"/>.
/// This is essential for the frontend tool termination pattern to work correctly.
/// </para>
/// </remarks>
internal sealed class AguiStreamingService : IAguiStreamingService
{
    private readonly IAguiMessageConverter _messageConverter;
    private readonly IAguiContextConverter _contextConverter;
    private readonly IAiRuntimeContextScopeProvider _runtimeContextScopeProvider;
    private readonly AiRuntimeContextContributorCollection _contextContributors;
    private readonly ILogger<AguiStreamingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AguiStreamingService"/> class.
    /// </summary>
    public AguiStreamingService(
        IAguiMessageConverter messageConverter,
        IAguiContextConverter contextConverter,
        IAiRuntimeContextScopeProvider runtimeContextScopeProvider,
        AiRuntimeContextContributorCollection contextContributors,
        ILogger<AguiStreamingService> logger)
    {
        _messageConverter = messageConverter;
        _contextConverter = contextConverter;
        _runtimeContextScopeProvider = runtimeContextScopeProvider;
        _contextContributors = contextContributors;
        _logger = logger;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IAguiEvent> StreamAgentAsync(
        AIAgent agent,
        AguiRunRequest request,
        IEnumerable<AITool>? frontendTools,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var emitter = new AguiEventEmitter(request.ThreadId, request.RunId);
        var frontendToolNames = frontendTools?.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Exception? streamError = null;

        // Emit RunStarted (outside try block)
        yield return emitter.EmitRunStarted();

        // Use manual enumerator pattern to avoid "yield in try with catch" limitation
        var coreStream = StreamCoreAsync(agent, request, emitter, frontendToolNames, cancellationToken);
        var enumerator = coreStream.GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {
                IAguiEvent? currentEvent;

                // Move next in inner try to catch streaming errors
                try
                {
                    if (!await enumerator.MoveNextAsync())
                        break;
                    currentEvent = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    // Let cancellation propagate without error event
                    throw;
                }
                catch (Exception ex)
                {
                    streamError = ex;
                    _logger.LogError(ex, "Error during agent streaming");
                    break;
                }

                // Yield outside try block (this is allowed)
                yield return currentEvent;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        // Emit error event if streaming failed
        if (streamError != null)
        {
            yield return emitter.EmitError(streamError.Message, "STREAMING_ERROR");
        }

        // Emit RunFinished with appropriate outcome
        yield return emitter.EmitRunFinished(streamError);
    }

    /// <summary>
    /// Core streaming logic that yields AG-UI events from the agent execution.
    /// This method does not handle errors - they propagate to the caller.
    /// </summary>
    private async IAsyncEnumerable<IAguiEvent> StreamCoreAsync(
        AIAgent agent,
        AguiRunRequest request,
        AguiEventEmitter emitter,
        HashSet<string> frontendToolNames,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Convert AG-UI context items to runtime context items
        var contextItems = _contextConverter.ConvertToRuntimeContextItems(request.Context);

        // Create runtime context scope - context is available via accessor until scope is disposed
        using var scope = _runtimeContextScopeProvider.CreateScope(contextItems);
        var runtimeContext = scope.Context;

        // Populate context with contributors
        _contextContributors.Populate(runtimeContext);

        // Build messages (may inject system message from context)
        var chatMessages = BuildChatMessages(request, runtimeContext);

        // Build agent options (sets ContentId for context resolution)
        var agentOptions = BuildAgentOptions(runtimeContext);

        _logger.LogDebug(
            "Starting agent streaming with {MessageCount} messages, {ToolCount} frontend tools",
            chatMessages.Count,
            frontendToolNames.Count);

        // Use MAF streaming with options (thread=null, options=agentOptions)
        await foreach (var update in agent.RunStreamingAsync(chatMessages, thread: null, options: agentOptions, cancellationToken: cancellationToken))
        {
            // Process content items (tool calls and results first, then text)
            if (update.Contents != null)
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case FunctionCallContent functionCall:
                            var toolCallEvent = ProcessFunctionCall(emitter, functionCall, frontendToolNames);
                            if (toolCallEvent != null)
                            {
                                yield return toolCallEvent;
                            }
                            break;

                        case FunctionResultContent functionResult:
                            var toolResultEvent = ProcessFunctionResult(emitter, functionResult);
                            if (toolResultEvent != null)
                            {
                                yield return toolResultEvent;
                            }
                            break;
                    }
                }
            }

            // Process text content
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return emitter.EmitTextChunk(update.Text);
            }
        }
    }

    /// <summary>
    /// Builds chat messages from the request, injecting system message from context if present.
    /// </summary>
    private List<ChatMessage> BuildChatMessages(AguiRunRequest request, AiRuntimeContext runtimeContext)
    {
        // Convert AG-UI messages
        var chatMessages = _messageConverter.ConvertToChatMessages(request.Messages);

        // Inject system message from context contributors
        if (runtimeContext.SystemMessageParts.Count > 0)
        {
            var contextContent = string.Join("\n\n", runtimeContext.SystemMessageParts);

            // Check if there's already a system message to combine with
            var existingSystemIndex = chatMessages.FindIndex(m => m.Role == ChatRole.System);
            if (existingSystemIndex >= 0)
            {
                // Prepend context content to existing system message
                var existingContent = chatMessages[existingSystemIndex].Text ?? string.Empty;
                var combinedContent = string.IsNullOrEmpty(existingContent)
                    ? contextContent
                    : $"{contextContent}\n\n{existingContent}";
                chatMessages[existingSystemIndex] = new ChatMessage(ChatRole.System, combinedContent);
            }
            else
            {
                // Insert new system message at the beginning
                chatMessages.Insert(0, new ChatMessage(ChatRole.System, contextContent));
            }
        }

        // Handle resume - inject tool results from resume payload
        if (request.Resume != null)
        {
            var resumeMessages = ExtractToolResultsFromResume(request.Resume);
            chatMessages.AddRange(resumeMessages);

            _logger.LogDebug(
                "Resume from interrupt {InterruptId} with {ResultCount} tool results",
                request.Resume.InterruptId,
                resumeMessages.Count);
        }

        return chatMessages;
    }

    /// <summary>
    /// Builds agent run options with ChatOptions for context resolution.
    /// </summary>
    private static AgentRunOptions? BuildAgentOptions(AiRuntimeContext runtimeContext)
    {
        // Extract EntityId for ContentContextResolver
        var entityId = runtimeContext.GetValue<Guid>(AiRuntimeContextKeys.EntityId);
        if (!entityId.HasValue)
        {
            return null;
        }

        var additionalProperties = new AdditionalPropertiesDictionary
        {
            [AiRuntimeContextKeys.ContentId] = entityId.Value
        };

        // Include ParentEntityId if available (for new entities)
        var parentEntityId = runtimeContext.GetValue<Guid>(AiRuntimeContextKeys.ParentEntityId);
        if (parentEntityId.HasValue)
        {
            additionalProperties[AiRuntimeContextKeys.ParentEntityId] = parentEntityId.Value;
        }

        return new ChatClientAgentRunOptions(new ChatOptions
        {
            AdditionalProperties = additionalProperties
        });
    }

    private IAguiEvent? ProcessFunctionCall(
        AguiEventEmitter emitter,
        FunctionCallContent functionCall,
        HashSet<string> frontendToolNames)
    {
        if (string.IsNullOrEmpty(functionCall.CallId))
            return null;

        var isFrontendTool = frontendToolNames.Contains(functionCall.Name);

        return emitter.EmitToolCall(
            functionCall.CallId,
            functionCall.Name,
            functionCall.Arguments,
            isFrontendTool);
    }

    private IAguiEvent? ProcessFunctionResult(
        AguiEventEmitter emitter,
        FunctionResultContent functionResult)
    {
        if (string.IsNullOrEmpty(functionResult.CallId))
            return null;

        return emitter.EmitToolResult(functionResult.CallId, functionResult.Result);
    }

    /// <summary>
    /// Extracts tool results from the resume payload and converts them to chat messages.
    /// </summary>
    /// <remarks>
    /// Expected payload format:
    /// <code>
    /// {
    ///   "toolResults": [
    ///     { "toolCallId": "call-1", "result": { ... } },
    ///     { "toolCallId": "call-2", "result": { ... } }
    ///   ]
    /// }
    /// </code>
    /// </remarks>
    private List<ChatMessage> ExtractToolResultsFromResume(AguiResumeInfo resume)
    {
        var results = new List<ChatMessage>();

        if (!resume.Payload.HasValue)
            return results;

        try
        {
            var payload = resume.Payload.Value;

            // Try to get toolResults array from payload
            if (payload.TryGetProperty("toolResults", out var toolResultsElement) &&
                toolResultsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var toolResultElement in toolResultsElement.EnumerateArray())
                {
                    if (toolResultElement.TryGetProperty("toolCallId", out var toolCallIdElement) &&
                        toolResultElement.TryGetProperty("result", out var resultElement))
                    {
                        var toolCallId = toolCallIdElement.GetString();
                        if (!string.IsNullOrEmpty(toolCallId))
                        {
                            var resultContent = new FunctionResultContent(toolCallId, resultElement);
                            results.Add(new ChatMessage(ChatRole.Tool, [resultContent]));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse resume payload for interrupt {InterruptId}", resume.InterruptId);
        }

        return results;
    }
}

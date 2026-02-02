using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Default implementation of <see cref="IAGUIStreamingService"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses direct streaming without <c>Task.Run()</c>, which preserves
/// AsyncLocal context including <see cref="FunctionInvokingChatClient.CurrentContext"/>.
/// This is essential for the frontend tool termination pattern to work correctly.
/// </para>
/// </remarks>
internal sealed class AGUIStreamingService : IAGUIStreamingService
{
    private readonly IAGUIMessageConverter _messageConverter;
    private readonly ILogger<AGUIStreamingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AGUIStreamingService"/> class.
    /// </summary>
    public AGUIStreamingService(
        IAGUIMessageConverter messageConverter,
        ILogger<AGUIStreamingService> logger)
    {
        _messageConverter = messageConverter;
        _logger = logger;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IAGUIEvent> StreamAgentAsync(
        AIAgent agent,
        AGUIRunRequest request,
        IEnumerable<AITool>? frontendTools,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var emitter = new AGUIEventEmitter(request.ThreadId, request.RunId);
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
                IAGUIEvent? currentEvent;

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
    private async IAsyncEnumerable<IAGUIEvent> StreamCoreAsync(
        AIAgent agent,
        AGUIRunRequest request,
        AGUIEventEmitter emitter,
        HashSet<string> frontendToolNames,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Convert AG-UI messages to M.E.AI chat messages
        var chatMessages = _messageConverter.ConvertToChatMessages(request.Messages);

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

        _logger.LogDebug(
            "Starting agent streaming with {MessageCount} messages, {ToolCount} frontend tools",
            chatMessages.Count,
            frontendToolNames.Count);

        // Use MAF streaming with options (session=null for new session)
        await foreach (var update in agent.RunStreamingAsync(chatMessages, session: null, cancellationToken: cancellationToken))
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


    private IAGUIEvent? ProcessFunctionCall(
        AGUIEventEmitter emitter,
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

    private IAGUIEvent? ProcessFunctionResult(
        AGUIEventEmitter emitter,
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
    private List<ChatMessage> ExtractToolResultsFromResume(AGUIResumeInfo resume)
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

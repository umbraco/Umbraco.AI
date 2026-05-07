using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Events.State;
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
    private readonly IAGUIFileProcessor _fileProcessor;
    private readonly ILogger<AGUIStreamingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AGUIStreamingService"/> class.
    /// </summary>
    public AGUIStreamingService(
        IAGUIMessageConverter messageConverter,
        IAGUIFileProcessor fileProcessor,
        ILogger<AGUIStreamingService> logger)
    {
        _messageConverter = messageConverter;
        _fileProcessor = fileProcessor;
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

        if (streamError != null)
        {
            yield return emitter.EmitError(streamError.Message, "STREAMING_ERROR");
        }
        else
        {
            yield return emitter.EmitRunFinished();
        }
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
        // Process file content: store base64, resolve id references
        var fileResult = await _fileProcessor.ProcessInboundAsync(request.Messages, emitter.ThreadId, cancellationToken);

        // Emit messages snapshot only when files were rewritten (base64 → id references).
        // This allows the frontend to adopt lightweight references for subsequent turns
        // without disrupting the conversation when no files are present.
        if (!ReferenceEquals(fileResult.RewrittenMessages, fileResult.ResolvedMessages))
        {
            yield return new MessagesSnapshotEvent
            {
                Messages = fileResult.RewrittenMessages,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        // Convert resolved messages (with bytes) to M.E.AI chat messages
        var chatMessages = _messageConverter.ConvertToChatMessages(fileResult.ResolvedMessages);

        // Handle resume — inject tool results from each resolved resume entry.
        // Per AG-UI spec the resume array contains one entry per open interrupt.
        if (request.Resume is { Count: > 0 })
        {
            var resumeMessages = ExtractToolResultsFromResume(request.Resume);
            chatMessages.AddRange(resumeMessages);

            _logger.LogDebug(
                "Resume with {EntryCount} entries produced {ResultCount} tool results",
                request.Resume.Count,
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
        // Don't filter out empty CallIds - let EmitToolCall handle ID generation
        // This fixes the Gemini empty CallId bug where CallId="" instead of null
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
        // Don't filter out empty CallIds - let EmitToolResult handle ID correlation
        // This fixes the Gemini empty CallId bug where CallId="" instead of null
        return emitter.EmitToolResult(functionResult.CallId, functionResult.Result);
    }

    /// <summary>
    /// Converts AG-UI resume entries into M.E.AI tool-result chat messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per AG-UI spec each resume entry maps 1:1 with an open interrupt the previous
    /// run emitted. For frontend tool-call interrupts we set <c>InterruptInfo.Id</c>
    /// equal to the <c>toolCallId</c> when emitting (see <c>AGUIEventEmitter</c>),
    /// so the resume entry's <c>InterruptId</c> recovers the original tool call id.
    /// </para>
    /// <para>
    /// Cancelled entries are skipped — we don't synthesise a tool result when the user
    /// abandoned the interrupt without input.
    /// </para>
    /// </remarks>
    private List<ChatMessage> ExtractToolResultsFromResume(IReadOnlyList<AGUIResumeEntry> resume)
    {
        var results = new List<ChatMessage>();

        foreach (var entry in resume)
        {
            if (entry.Status != AGUIResumeStatus.Resolved)
            {
                continue;
            }

            if (string.IsNullOrEmpty(entry.InterruptId) || !entry.Payload.HasValue)
            {
                _logger.LogWarning(
                    "Resume entry {InterruptId} resolved without a payload; skipping",
                    entry.InterruptId);
                continue;
            }

            // InterruptId is the toolCallId for tool_call interrupts (see AGUIEventEmitter).
            var resultContent = new FunctionResultContent(entry.InterruptId, entry.Payload.Value);
            results.Add(new ChatMessage(ChatRole.Tool, [resultContent]));
        }

        return results;
    }
}

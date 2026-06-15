using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Events.State;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Umbraco.AI.Core.Providers.Errors;

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

        // Emit error event if streaming failed.
        // Provider SDK failures arrive pre-classified as AIProviderException (the chat client is
        // wrapped by the error-classifying decorator in the capability factory), carrying a
        // user-safe message and a stable category code for retry affordances. Anything else is an
        // application-layer failure we surface generically without leaking raw exception text.
        if (streamError != null)
        {
            string userMessage;
            string code;
            if (FindProviderException(streamError) is { } providerError)
            {
                userMessage = providerError.UserMessage;
                code = providerError.Category.ToString();
                _logger.LogError(streamError,
                    "Agent run {RunId} failed. Category={Category}, ProviderCode={ProviderCode}",
                    request.RunId, providerError.Category, providerError.ProviderCode);
            }
            else
            {
                userMessage = "An unexpected error occurred. Please try again.";
                code = AIProviderErrorCategory.Unknown.ToString();
                _logger.LogError(streamError,
                    "Agent run {RunId} failed with an unclassified error.", request.RunId);
            }

            yield return emitter.EmitError(userMessage, code);
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
                            // Diagnostic: this log line is the smoking gun for "model
                            // generated a tool_use but no TOOL_CALL_CHUNK reached the
                            // frontend". If the upstream AIToolReorderingChatClient
                            // logged the buffered call but this line never fires, the
                            // FunctionInvokingChatClient is consuming the call without
                            // forwarding it.
                            _logger.LogInformation(
                                "AGUIStreamingService received FunctionCallContent for tool '{ToolName}' (callId={CallId}, isFrontend={IsFrontend}) on run {RunId}.",
                                functionCall.Name,
                                functionCall.CallId,
                                frontendToolNames.Contains(functionCall.Name),
                                request.RunId);
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

                        case ErrorContent errorContent:
                            // Providers stream ErrorContent for non-fatal errors that occur
                            // mid-response (content filters, transient model errors, function-
                            // call sanitisation when IncludeDetailedErrors is false, etc.). If
                            // we drop them silently the chat trace renders a bare
                            // '[unknown:ErrorContent]' with no body and the underlying cause
                            // never reaches the logs. Log first, then surface inline so the
                            // user sees what happened and the run can continue.
                            _logger.LogError(
                                "Provider streamed ErrorContent during run {RunId}. Code: {ErrorCode}, Message: {Message}, Details: {Details}",
                                request.RunId,
                                errorContent.ErrorCode ?? "(none)",
                                errorContent.Message ?? "(empty)",
                                errorContent.Details ?? "(none)");
                            yield return emitter.EmitTextChunk(FormatProviderErrorForChat(errorContent));
                            break;

                        case TextContent:
                            // Already aggregated below via update.Text — skip to avoid double-emit.
                            break;

                        default:
                            // Future-proof: any AIContent subtype MEAI adds later gets logged at
                            // debug level so it doesn't vanish silently.
                            _logger.LogDebug(
                                "Unhandled AIContent type '{ContentType}' in stream for run {RunId}",
                                content.GetType().Name,
                                request.RunId);
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
    /// Finds the classified <see cref="AIProviderException"/> in the exception chain, if any. The
    /// error-classifying client decorator throws it directly, but agent/middleware layers above may
    /// wrap it, so we walk inner exceptions rather than only checking the top.
    /// </summary>
    private static AIProviderException? FindProviderException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is AIProviderException providerError)
            {
                return providerError;
            }
        }

        return null;
    }

    private static string FormatProviderErrorForChat(ErrorContent errorContent)
    {
        var message = string.IsNullOrEmpty(errorContent.Message) ? "(no message)" : errorContent.Message;
        return string.IsNullOrEmpty(errorContent.ErrorCode)
            ? $"\n\n[Provider error: {message}]\n\n"
            : $"\n\n[Provider error {errorContent.ErrorCode}: {message}]\n\n";
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

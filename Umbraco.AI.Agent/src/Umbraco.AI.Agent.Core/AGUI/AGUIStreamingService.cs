using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Events.State;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Core.Tools;

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
    private readonly AIToolCollection _toolCollection;
    private readonly ILogger<AGUIStreamingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AGUIStreamingService"/> class.
    /// </summary>
    public AGUIStreamingService(
        IAGUIMessageConverter messageConverter,
        IAGUIFileProcessor fileProcessor,
        AIToolCollection toolCollection,
        ILogger<AGUIStreamingService> logger)
    {
        _messageConverter = messageConverter;
        _fileProcessor = fileProcessor;
        _toolCollection = toolCollection;
        _logger = logger;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IAGUIEvent> StreamAgentAsync(
        AIAgent agent,
        AGUIRunRequest request,
        IEnumerable<AITool>? frontendTools,
        CancellationToken cancellationToken = default)
        => StreamAgentAsync(agent, request, frontendTools, session: null, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<IAGUIEvent> StreamAgentAsync(
        AIAgent agent,
        AGUIRunRequest request,
        IEnumerable<AITool>? frontendTools,
        AgentSession? session,
        IReadOnlyDictionary<string, ToolApprovalRequestContent>? pendingApprovalCalls = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var emitter = new AGUIEventEmitter(request.ThreadId, request.RunId);
        var frontendToolNames = frontendTools?.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Exception? streamError = null;

        // Emit RunStarted (outside try block)
        yield return emitter.EmitRunStarted();

        // Use manual enumerator pattern to avoid "yield in try with catch" limitation
        var coreStream = StreamCoreAsync(agent, request, emitter, frontendToolNames, session, pendingApprovalCalls, cancellationToken);
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
        AgentSession? session,
        IReadOnlyDictionary<string, ToolApprovalRequestContent>? pendingApprovalCalls,
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
            // Promote FunctionCallContent → ToolApprovalRequestContent in the converted history
            // for any approval interrupt. FICC needs the original request present in the
            // replayed history to correlate the ToolApprovalResponseContent (spike Finding B).
            //
            // Only do this when NO persisted ChatHistoryProvider is bound (session is null). When a
            // session IS bound (Copilot Workspace), MAF's ChatHistoryProvider.InvokingCoreAsync
            // unconditionally CONCATENATES the provider's persisted history in front of whatever we
            // pass here. The interrupted turn's assistant message was already persisted with the real
            // ToolApprovalRequestContent (FICC's own output when the run paused), so the provider
            // already supplies one copy. The client ALSO replays that same turn's tool call in
            // chatMessages (Task 5's onToolCallStart/onToolCallArgsEnd capture — needed so a
            // non-session, stateless resume can correlate it). Left in place for a session-bound run,
            // that client copy becomes a SECOND copy of the same tool call once concatenated with
            // persisted history: promoting it produces a duplicate ToolApprovalRequestContent (FICC
            // throws "...that have no matching ToolApprovalResponseContent" on the one left over after
            // a single response matches the other), and even without promoting it, the raw duplicate
            // FunctionCallContent still reaches the wire as a second tool_use block with the same id,
            // which the provider itself rejects (observed: Anthropic 400 "tool_use ids must be
            // unique"). So for a session-bound run we don't promote AND we strip the client's copy
            // outright, relying solely on the persisted-history copy MAF concatenates in.
            if (session is null)
            {
                PromoteApprovalRequestsInHistory(chatMessages, request.Resume);
            }
            else if (pendingApprovalCalls is { Count: > 0 })
            {
                RemovePersistedApprovalCallsFromClientHistory(chatMessages, pendingApprovalCalls.Keys);
            }

            var resumeMessages = ExtractToolResultsFromResume(chatMessages, request.Resume, pendingApprovalCalls, request.RunId);
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

        // Use MAF streaming. A null session starts a fresh one (contextual Copilot); a bound session
        // (Copilot Workspace) drives the attached ChatHistoryProvider against its conversation.
        await foreach (var update in agent.RunStreamingAsync(chatMessages, session: session, cancellationToken: cancellationToken))
        {
            // Process content items (tool calls and results first, then text)
            if (update.Contents != null)
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case ToolApprovalRequestContent approvalRequest:
                            // MEAI's FunctionInvokingChatClient emits this instead of executing
                            // a destructive tool wrapped in ApprovalRequiredAIFunction.
                            // Emit the tool call event so the frontend sees the pending call,
                            // then register the approval interrupt for EmitRunFinished.
                            if (approvalRequest.ToolCall is FunctionCallContent pendingCall)
                            {
                                var approvalInterruptId = AGUIInterruptKind.ForApproval(pendingCall.CallId);
                                var argsJson = pendingCall.Arguments is not null
                                    ? System.Text.Json.JsonSerializer.Serialize(pendingCall.Arguments)
                                    : "{}";
                                var pendingEvent = emitter.EmitToolCall(pendingCall.CallId, pendingCall.Name, pendingCall.Arguments, isFrontendTool: false);
                                if (pendingEvent != null)
                                {
                                    yield return pendingEvent;
                                }

                                var tool = _toolCollection.GetById(pendingCall.Name);
                                var approvalTitle = tool?.Name ?? pendingCall.Name;
                                var approvalMessage = tool is not null
                                    ? await tool.DescribeInvocationAsync(pendingCall.Arguments) ?? FormatGenericArgsMessage(pendingCall.Arguments)
                                    : FormatGenericArgsMessage(pendingCall.Arguments);
                                var confirmationPhrase = tool is not null
                                    ? await tool.ResolveConfirmationPhraseAsync(pendingCall.Arguments)
                                    : null;
                                emitter.RegisterApprovalRequest(approvalInterruptId, pendingCall.CallId, pendingCall.Name, argsJson, approvalTitle, approvalMessage, confirmationPhrase);
                            }
                            break;

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

    /// <summary>
    /// Builds a generic "what this call will do" message from raw arguments, for tools that haven't
    /// implemented <see cref="IAITool.DescribeInvocationAsync"/> -- a plain list of argument name/value
    /// pairs is still far more informative to a human approving the call than the bare tool name alone.
    /// </summary>
    private static string FormatGenericArgsMessage(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return "This action takes no arguments.";
        }

        var parts = arguments.Select(kvp => $"{kvp.Key}: {System.Text.Json.JsonSerializer.Serialize(kvp.Value)}");
        return string.Join(", ", parts);
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
    /// Promotes <see cref="FunctionCallContent"/> to <see cref="ToolApprovalRequestContent"/>
    /// in-place for any assistant message whose tool call is covered by an approval resume entry.
    /// FICC requires the original approval request to be present in the replayed history so it
    /// can correlate the matching <see cref="ToolApprovalResponseContent"/> (spike Finding B).
    /// </summary>
    private static void PromoteApprovalRequestsInHistory(
        List<ChatMessage> chatMessages,
        IReadOnlyList<AGUIResumeEntry> resume)
    {
        var approvalCallIds = resume
            .Where(e => AGUIInterruptKind.IsApproval(e.InterruptId))
            .Select(e => AGUIInterruptKind.GetCallId(e.InterruptId)!)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (approvalCallIds.Count == 0) return;

        for (var i = 0; i < chatMessages.Count; i++)
        {
            var msg = chatMessages[i];
            if (msg.Role != ChatRole.Assistant || msg.Contents is null) continue;

            var modified = false;
            var newContents = new List<AIContent>(msg.Contents.Count);

            foreach (var content in msg.Contents)
            {
                if (content is FunctionCallContent fc && approvalCallIds.Contains(fc.CallId))
                {
                    newContents.Add(new ToolApprovalRequestContent(fc.CallId, fc));
                    modified = true;
                }
                else
                {
                    newContents.Add(content);
                }
            }

            if (modified)
            {
                chatMessages[i] = new ChatMessage(ChatRole.Assistant, newContents)
                {
                    MessageId = msg.MessageId
                };
            }
        }
    }

    /// <summary>
    /// Strips <see cref="FunctionCallContent"/> for the given <paramref name="callIds"/> out of the
    /// client-resent <paramref name="chatMessages"/> in-place, dropping a message entirely once it has
    /// no content left. Used on a session-bound resume: the caller has already recovered these exact
    /// calls from the conversation's persisted history (<paramref name="callIds"/> is
    /// <c>pendingApprovalCalls.Keys</c>), and that persisted copy is what MAF's bound
    /// <c>ChatHistoryProvider</c> concatenates in ahead of these messages. Leaving the client's copy in
    /// place would send the same tool call twice — the wire-level duplicate a real provider rejects
    /// (Anthropic: "tool_use ids must be unique") even after <see cref="PromoteApprovalRequestsInHistory"/>
    /// is skipped for this path.
    /// </summary>
    private static void RemovePersistedApprovalCallsFromClientHistory(
        List<ChatMessage> chatMessages,
        IEnumerable<string> callIds)
    {
        var ids = callIds as ICollection<string> ?? callIds.ToList();
        if (ids.Count == 0) return;

        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var msg = chatMessages[i];
            if (msg.Role != ChatRole.Assistant || msg.Contents is null) continue;

            var filtered = msg.Contents
                .Where(c => c is not FunctionCallContent fc || !ids.Contains(fc.CallId))
                .ToList();

            if (filtered.Count == msg.Contents.Count) continue;

            if (filtered.Count == 0)
            {
                chatMessages.RemoveAt(i);
            }
            else
            {
                chatMessages[i] = new ChatMessage(ChatRole.Assistant, filtered)
                {
                    MessageId = msg.MessageId
                };
            }
        }
    }

    /// <summary>
    /// Converts AG-UI resume entries into M.E.AI chat messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per AG-UI spec each resume entry maps 1:1 with an open interrupt the previous
    /// run emitted. Two interrupt kinds are handled:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <b>Approval interrupts</b> (<c>"approval:callId"</c> prefix): the payload carries
    ///     <c>{ "approved": bool }</c>. Produces a <see cref="ToolApprovalResponseContent"/>
    ///     on <see cref="ChatRole.User"/> so FICC executes (approved) or skips (denied)
    ///     the wrapped <see cref="ApprovalRequiredAIFunction"/> on the next invocation.
    ///   </item>
    ///   <item>
    ///     <b>Tool-call interrupts</b> (no prefix): the interrupt id equals the tool call id.
    ///     Produces a <see cref="FunctionResultContent"/> on <see cref="ChatRole.Tool"/>
    ///     (unchanged from the original frontend-tool resume path).
    ///   </item>
    /// </list>
    /// <para>
    /// Cancelled entries are skipped — we don't synthesise a result when the user
    /// abandoned the interrupt without input.
    /// </para>
    /// </remarks>
    private List<ChatMessage> ExtractToolResultsFromResume(
        IReadOnlyList<ChatMessage> chatMessages,
        IReadOnlyList<AGUIResumeEntry> resume,
        IReadOnlyDictionary<string, ToolApprovalRequestContent>? pendingApprovalCalls,
        string runId)
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

            if (AGUIInterruptKind.IsApproval(entry.InterruptId))
            {
                // Backend tool approval interrupt: payload is { "approved": bool }.
                var callId = AGUIInterruptKind.GetCallId(entry.InterruptId)!;
                var approved = entry.Payload.Value.TryGetProperty("approved", out var ap)
                    && ap.ValueKind == System.Text.Json.JsonValueKind.True;

                // Recover the ORIGINAL approval request (name + arguments, and — critically — FICC's own
                // RequestId, e.g. "ficc_<callId>", NOT the callId itself) so the response is built via
                // CreateResponse() below rather than hand-constructed with a guessed id. Microsoft.Agents.AI's
                // ApprovalResponseBindingChatClient matches inbound responses against its session-recorded
                // pending requests by RequestId and silently drops any response that doesn't match — a response
                // built with the wrong id looks identical to a forged one. Look in the replayed client history
                // first (in-flight resume), then the persisted history the provider will load (resume after a
                // reload, B2).
                var requestedApprovalRequest = FindApprovalToolCall(chatMessages, callId);
                if (requestedApprovalRequest is null && pendingApprovalCalls is not null)
                {
                    pendingApprovalCalls.TryGetValue(callId, out requestedApprovalRequest);
                }

                if (requestedApprovalRequest is null)
                {
                    // Not correlatable to a pending tool call in client OR persisted history — a stale or
                    // duplicate resume entry (e.g. from a prior run). Skip it rather than synthesise an
                    // empty call that FICC would try and fail to invoke (B2).
                    _logger.LogWarning(
                        "Resume approval for callId {CallId} on run {RunId} has no matching pending tool " +
                        "call in client or persisted history; skipping as stale.",
                        callId, runId);
                    continue;
                }

                results.Add(new ChatMessage(ChatRole.User, [requestedApprovalRequest.CreateResponse(approved)]));
                continue;
            }

            // Frontend tool_call interrupt (unchanged): InterruptId == toolCallId.
            var resultContent = new FunctionResultContent(entry.InterruptId, entry.Payload.Value);
            results.Add(new ChatMessage(ChatRole.Tool, [resultContent]));
        }

        return results;
    }

    /// <summary>
    /// Finds the original <see cref="ToolApprovalRequestContent"/> for an approval <paramref name="callId"/>
    /// in the replayed history, or null if absent.
    /// </summary>
    private static ToolApprovalRequestContent? FindApprovalToolCall(IReadOnlyList<ChatMessage> chatMessages, string callId)
        => chatMessages
            .SelectMany(m => m.Contents ?? [])
            .OfType<ToolApprovalRequestContent>()
            .FirstOrDefault(c => c.ToolCall.CallId == callId);
}

using System.Text.Json;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Events.Lifecycle;
using Umbraco.AI.AGUI.Events.Messages;
using Umbraco.AI.AGUI.Events.Tools;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.AGUI.Streaming;

/// <summary>
/// Helper class for emitting AG-UI events with consistent ID management.
/// </summary>
/// <remarks>
/// <para>
/// This class manages the lifecycle of message and tool IDs during streaming,
/// ensuring proper correlation between events and supporting multi-block UI layouts.
/// </para>
/// <para>
/// Key behaviors:
/// <list type="bullet">
///   <item>Tracks frontend tool call IDs to skip emitting their results</item>
///   <item>Regenerates message ID after tool results for multi-block UI support</item>
///   <item>Determines run outcome based on frontend tool presence</item>
/// </list>
/// </para>
/// </remarks>
public sealed class AGUIEventEmitter
{
    private readonly string _threadId;
    private readonly string _runId;
    private readonly HashSet<string> _emittedToolCallIds = new();
    private readonly HashSet<string> _frontendToolCallIds = new();

    private string _currentMessageId;
    private string? _lastGeneratedCallId;

    /// <summary>
    /// Initializes a new instance of the <see cref="AGUIEventEmitter"/> class.
    /// </summary>
    /// <param name="threadId">The thread identifier for this conversation.</param>
    /// <param name="runId">The run identifier for this execution.</param>
    public AGUIEventEmitter(string threadId, string runId)
    {
        _threadId = string.IsNullOrEmpty(threadId) ? Guid.NewGuid().ToString() : threadId;
        _runId = string.IsNullOrEmpty(runId) ? Guid.NewGuid().ToString() : runId;
        _currentMessageId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId => _threadId;

    /// <summary>
    /// Gets the run identifier.
    /// </summary>
    public string RunId => _runId;

    /// <summary>
    /// Gets the current message identifier.
    /// </summary>
    public string CurrentMessageId => _currentMessageId;

    /// <summary>
    /// Gets whether any frontend tool calls have been emitted.
    /// </summary>
    public bool HasFrontendToolCalls => _frontendToolCallIds.Count > 0;

    /// <summary>
    /// Gets the set of frontend tool call IDs that have been emitted.
    /// </summary>
    public IReadOnlySet<string> FrontendToolCallIds => _frontendToolCallIds;

    /// <summary>
    /// Emits a <see cref="RunStartedEvent"/>.
    /// </summary>
    /// <returns>The run started event.</returns>
    public RunStartedEvent EmitRunStarted() => new()
    {
        ThreadId = _threadId,
        RunId = _runId,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };

    /// <summary>
    /// Emits a <see cref="TextMessageChunkEvent"/> for streaming text content.
    /// </summary>
    /// <param name="delta">The text content delta.</param>
    /// <returns>The text message chunk event.</returns>
    public TextMessageChunkEvent EmitTextChunk(string delta) => new()
    {
        MessageId = _currentMessageId,
        Role = AGUITextMessageRole.Assistant,
        Delta = delta,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };

    /// <summary>
    /// Emits a <see cref="ToolCallChunkEvent"/> for a tool call.
    /// </summary>
    /// <param name="toolCallId">The tool call identifier (may be null or empty for providers like Gemini).</param>
    /// <param name="toolCallName">The name of the tool being called.</param>
    /// <param name="arguments">The tool arguments (will be serialized to JSON).</param>
    /// <param name="isFrontendTool">Whether this is a frontend tool (execution on client).</param>
    /// <returns>The tool call chunk event, or <c>null</c> if this tool call was already emitted.</returns>
    /// <remarks>
    /// If the provider doesn't supply a CallId (e.g., Google Gemini), a unique ID is generated
    /// and tracked for correlation with the immediate subsequent tool result.
    /// </remarks>
    public ToolCallChunkEvent? EmitToolCall(
        string? toolCallId,
        string toolCallName,
        object? arguments,
        bool isFrontendTool)
    {
        // Generate ID if provider doesn't supply one (workaround for Gemini empty CallId bug)
        var effectiveCallId = string.IsNullOrEmpty(toolCallId)
            ? $"generated-{Guid.NewGuid()}"
            : toolCallId;

        // Track generated ID for result correlation
        if (string.IsNullOrEmpty(toolCallId))
        {
            _lastGeneratedCallId = effectiveCallId;
        }

        // Skip if already emitted (deduplication)
        if (!_emittedToolCallIds.Add(effectiveCallId))
            return null;

        // Track frontend tools for outcome determination
        if (isFrontendTool)
        {
            _frontendToolCallIds.Add(effectiveCallId);
        }

        var argsJson = arguments != null
            ? JsonSerializer.Serialize(arguments)
            : "{}";

        return new ToolCallChunkEvent
        {
            ToolCallId = effectiveCallId,
            ToolCallName = toolCallName,
            ParentMessageId = _currentMessageId,
            Delta = argsJson,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// Emits a <see cref="ToolCallResultEvent"/> for a tool result.
    /// </summary>
    /// <param name="toolCallId">The tool call identifier (may be null or empty for providers like Gemini).</param>
    /// <param name="result">The tool result (will be serialized to JSON).</param>
    /// <returns>The tool call result event, or <c>null</c> if this is a frontend tool result.</returns>
    /// <remarks>
    /// <para>
    /// Frontend tool results are not emitted because the client executes them.
    /// After emitting a tool result, a new message ID is generated to support
    /// multi-block UI layouts.
    /// </para>
    /// <para>
    /// If the provider doesn't supply a CallId (e.g., Google Gemini), this method
    /// uses the last generated ID from <see cref="EmitToolCall"/> to correlate the result.
    /// </para>
    /// </remarks>
    public ToolCallResultEvent? EmitToolResult(string? toolCallId, object? result)
    {
        // Use last generated ID if provider doesn't supply one (workaround for Gemini)
        var effectiveCallId = string.IsNullOrEmpty(toolCallId)
            ? _lastGeneratedCallId
            : toolCallId;

        if (string.IsNullOrEmpty(effectiveCallId))
            return null;

        // Consume the generated ID after use
        if (string.IsNullOrEmpty(toolCallId))
        {
            _lastGeneratedCallId = null;
        }

        // Skip frontend tool results - client executes them
        if (_frontendToolCallIds.Contains(effectiveCallId))
            return null;

        var resultJson = result != null
            ? JsonSerializer.Serialize(result)
            : "null";

        // Generate new message ID for multi-block UI
        var resultMessageId = Guid.NewGuid().ToString();
        _currentMessageId = Guid.NewGuid().ToString();

        return new ToolCallResultEvent
        {
            MessageId = resultMessageId,
            ToolCallId = effectiveCallId,
            Content = resultJson,
            Role = AGUIToolCallRole.Tool,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// Emits a <see cref="RunErrorEvent"/> for an error that occurred during streaming.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The optional error code.</param>
    /// <returns>The run error event.</returns>
    public RunErrorEvent EmitError(string message, string? code = null) => new()
    {
        Message = message,
        Code = code,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };

    /// <summary>
    /// Emits a <see cref="RunFinishedEvent"/> with the appropriate outcome.
    /// </summary>
    /// <returns>The run finished event.</returns>
    /// <remarks>
    /// <para>
    /// The outcome is determined as follows:
    /// <list type="bullet">
    ///   <item>If frontend tools were called: <see cref="AGUIRunOutcomeInterrupt"/>
    ///   with one entry per pending frontend tool call (<c>reason="tool_call"</c>).</item>
    ///   <item>Otherwise: <see cref="AGUIRunOutcomeSuccess"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Errors are NOT a valid outcome of <c>RUN_FINISHED</c> per AG-UI spec — emit a
    /// <see cref="RunErrorEvent"/> via <see cref="EmitError"/> and terminate the stream
    /// without a subsequent <c>RUN_FINISHED</c>.
    /// </para>
    /// </remarks>
    public RunFinishedEvent EmitRunFinished()
    {
        AGUIRunOutcome outcome = HasFrontendToolCalls
            ? new AGUIRunOutcomeInterrupt(
                _frontendToolCallIds
                    .Select(toolCallId => new AGUIInterruptInfo
                    {
                        // Use toolCallId as the interrupt id so the server can recover
                        // the corresponding tool call from a resume entry without
                        // having to track an interruptId -> toolCallId mapping.
                        Id = toolCallId,
                        Reason = "tool_call",
                        ToolCallId = toolCallId,
                    })
                    .ToList())
            : new AGUIRunOutcomeSuccess();

        return new RunFinishedEvent
        {
            ThreadId = _threadId,
            RunId = _runId,
            Outcome = outcome,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// Checks if a tool call ID has already been emitted.
    /// </summary>
    /// <param name="toolCallId">The tool call identifier to check.</param>
    /// <returns><c>true</c> if the tool call was already emitted; otherwise, <c>false</c>.</returns>
    public bool HasEmittedToolCall(string toolCallId) => _emittedToolCallIds.Contains(toolCallId);

    /// <summary>
    /// Checks if a tool call ID is a frontend tool.
    /// </summary>
    /// <param name="toolCallId">The tool call identifier to check.</param>
    /// <returns><c>true</c> if this is a frontend tool call; otherwise, <c>false</c>.</returns>
    public bool IsFrontendToolCall(string toolCallId) => _frontendToolCallIds.Contains(toolCallId);

    /// <summary>
    /// Regenerates the current message ID. Use this when you need a new message block
    /// without emitting a tool result.
    /// </summary>
    public void RegenerateMessageId() => _currentMessageId = Guid.NewGuid().ToString();
}

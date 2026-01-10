using System.Text.Json;
using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Events.Lifecycle;
using Umbraco.Ai.Agui.Events.Messages;
using Umbraco.Ai.Agui.Events.Tools;
using Umbraco.Ai.Agui.Models;

namespace Umbraco.Ai.Agui.Streaming;

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
public sealed class AguiEventEmitter
{
    private readonly string _threadId;
    private readonly string _runId;
    private readonly HashSet<string> _emittedToolCallIds = new();
    private readonly HashSet<string> _frontendToolCallIds = new();

    private string _currentMessageId;

    /// <summary>
    /// Initializes a new instance of the <see cref="AguiEventEmitter"/> class.
    /// </summary>
    /// <param name="threadId">The thread identifier for this conversation.</param>
    /// <param name="runId">The run identifier for this execution.</param>
    public AguiEventEmitter(string threadId, string runId)
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
        Role = AguiMessageRole.Assistant,
        Delta = delta,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };

    /// <summary>
    /// Emits a <see cref="ToolCallChunkEvent"/> for a tool call.
    /// </summary>
    /// <param name="toolCallId">The tool call identifier.</param>
    /// <param name="toolCallName">The name of the tool being called.</param>
    /// <param name="arguments">The tool arguments (will be serialized to JSON).</param>
    /// <param name="isFrontendTool">Whether this is a frontend tool (execution on client).</param>
    /// <returns>The tool call chunk event, or <c>null</c> if this tool call was already emitted.</returns>
    public ToolCallChunkEvent? EmitToolCall(
        string toolCallId,
        string toolCallName,
        object? arguments,
        bool isFrontendTool)
    {
        if (string.IsNullOrEmpty(toolCallId))
            return null;

        // Skip if already emitted
        if (!_emittedToolCallIds.Add(toolCallId))
            return null;

        // Track frontend tools for outcome determination
        if (isFrontendTool)
        {
            _frontendToolCallIds.Add(toolCallId);
        }

        var argsJson = arguments != null
            ? JsonSerializer.Serialize(arguments)
            : "{}";

        return new ToolCallChunkEvent
        {
            ToolCallId = toolCallId,
            ToolCallName = toolCallName,
            ParentMessageId = _currentMessageId,
            Delta = argsJson,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// Emits a <see cref="ToolCallResultEvent"/> for a tool result.
    /// </summary>
    /// <param name="toolCallId">The tool call identifier.</param>
    /// <param name="result">The tool result (will be serialized to JSON).</param>
    /// <returns>The tool call result event, or <c>null</c> if this is a frontend tool result.</returns>
    /// <remarks>
    /// Frontend tool results are not emitted because the client executes them.
    /// After emitting a tool result, a new message ID is generated to support
    /// multi-block UI layouts.
    /// </remarks>
    public ToolCallResultEvent? EmitToolResult(string toolCallId, object? result)
    {
        if (string.IsNullOrEmpty(toolCallId))
            return null;

        // Skip frontend tool results - client executes them
        if (_frontendToolCallIds.Contains(toolCallId))
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
            ToolCallId = toolCallId,
            Content = resultJson,
            Role = AguiMessageRole.Tool,
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
    /// <param name="error">Optional exception if an error occurred.</param>
    /// <returns>The run finished event.</returns>
    /// <remarks>
    /// The outcome is determined as follows:
    /// <list type="bullet">
    ///   <item>If <paramref name="error"/> is provided: <see cref="AguiRunOutcome.Error"/></item>
    ///   <item>If frontend tools were called: <see cref="AguiRunOutcome.Interrupt"/></item>
    ///   <item>Otherwise: <see cref="AguiRunOutcome.Success"/></item>
    /// </list>
    /// </remarks>
    public RunFinishedEvent EmitRunFinished(Exception? error = null)
    {
        var outcome = error != null
            ? AguiRunOutcome.Error
            : HasFrontendToolCalls
                ? AguiRunOutcome.Interrupt
                : AguiRunOutcome.Success;

        AguiInterruptInfo? interrupt = null;
        if (outcome == AguiRunOutcome.Interrupt)
        {
            interrupt = new AguiInterruptInfo
            {
                Id = Guid.NewGuid().ToString(),
                Reason = "tool_execution"
            };
        }

        return new RunFinishedEvent
        {
            ThreadId = _threadId,
            RunId = _runId,
            Outcome = outcome,
            Interrupt = interrupt,
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

using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Processes file content in AG-UI messages.
/// Handles storing base64 data on first upload and resolving file references on subsequent turns.
/// </summary>
public interface IAGUIFileProcessor
{
    /// <summary>
    /// Processes inbound messages: stores base64 data and resolves file ID references.
    /// </summary>
    /// <param name="messages">The AG-UI messages to process.</param>
    /// <param name="threadId">The conversation thread ID for file scoping.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A result containing two views of the messages:
    /// <list type="bullet">
    ///   <item><see cref="AGUIFileProcessorResult.RewrittenMessages"/> — base64 data swapped to ID references (for MessagesSnapshotEvent)</item>
    ///   <item><see cref="AGUIFileProcessorResult.ResolvedMessages"/> — binary content resolved to bytes (for converter → LLM)</item>
    /// </list>
    /// </returns>
    Task<AGUIFileProcessorResult> ProcessInboundAsync(
        IEnumerable<AGUIMessage>? messages,
        string threadId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of file processing containing two views of the messages.
/// </summary>
public sealed class AGUIFileProcessorResult
{
    /// <summary>
    /// Messages with base64 data swapped to ID references (lightweight, for snapshot events).
    /// </summary>
    public required IEnumerable<AGUIMessage> RewrittenMessages { get; init; }

    /// <summary>
    /// Messages with binary content resolved to raw bytes (for converter → LLM).
    /// </summary>
    public required IEnumerable<AGUIMessage> ResolvedMessages { get; init; }
}

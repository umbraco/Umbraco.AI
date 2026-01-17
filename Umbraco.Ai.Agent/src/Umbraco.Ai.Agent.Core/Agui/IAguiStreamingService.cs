using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Models;

namespace Umbraco.Ai.Agent.Core.Agui;

/// <summary>
/// Service for streaming AG-UI events from an AI agent.
/// </summary>
/// <remarks>
/// <para>
/// This service handles the core streaming logic, including:
/// <list type="bullet">
///   <item>Converting AG-UI messages to M.E.AI chat messages</item>
///   <item>Running the MAF agent with streaming</item>
///   <item>Emitting appropriate AG-UI events for text, tool calls, and tool results</item>
///   <item>Handling resume flow for continuing after frontend tool interrupts</item>
///   <item>Determining run outcome based on frontend tool presence</item>
/// </list>
/// </para>
/// <para>
/// This service does NOT use <c>Task.Run()</c>, preserving AsyncLocal context
/// (such as <see cref="FunctionInvokingChatClient.CurrentContext"/>).
/// </para>
/// </remarks>
public interface IAguiStreamingService
{
    /// <summary>
    /// Streams AG-UI events from an AI agent execution.
    /// </summary>
    /// <param name="agent">The MAF AIAgent to run.</param>
    /// <param name="request">The AG-UI run request containing messages, tools, and context.</param>
    /// <param name="frontendTools">The frontend tools (converted from request.Tools).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of AG-UI events.</returns>
    IAsyncEnumerable<IAguiEvent> StreamAgentAsync(
        AIAgent agent,
        AguiRunRequest request,
        IEnumerable<AITool>? frontendTools,
        CancellationToken cancellationToken);
}

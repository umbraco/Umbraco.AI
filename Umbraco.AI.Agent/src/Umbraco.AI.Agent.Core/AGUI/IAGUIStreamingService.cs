using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.Agent.Core.AGUI;

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
public interface IAGUIStreamingService
{
    /// <summary>
    /// Streams AG-UI events from an AI agent execution.
    /// </summary>
    /// <param name="agent">The MAF AIAgent to run.</param>
    /// <param name="request">The AG-UI run request containing messages, tools, and context.</param>
    /// <param name="frontendTools">The frontend tools (converted from request.Tools).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of AG-UI events.</returns>
    /// <remarks>
    /// System message injection is handled automatically by the agent.
    /// The agent should be created using <see cref="IAIAgentFactory.CreateAgentAsync"/> to ensure
    /// runtime context contributors populate system message parts correctly.
    /// </remarks>
    IAsyncEnumerable<IAGUIEvent> StreamAgentAsync(
        AIAgent agent,
        AGUIRunRequest request,
        IEnumerable<AITool>? frontendTools,
        CancellationToken cancellationToken = default);
}

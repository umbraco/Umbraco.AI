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

    /// <summary>
    /// Streams AG-UI events as
    /// <see cref="StreamAgentAsync(AIAgent, AGUIRunRequest, IEnumerable{AITool}, CancellationToken)"/>,
    /// additionally binding the run to an existing MAF <paramref name="session"/>. Surfaces with
    /// server-side conversation persistence (Copilot Workspace) pass a conversation-bound session so
    /// the attached <c>ChatHistoryProvider</c> loads/stores against the right conversation; the
    /// contextual Copilot passes <see langword="null"/> and behaves exactly as before.
    /// </summary>
    /// <param name="agent">The MAF AIAgent to run.</param>
    /// <param name="request">The AG-UI run request containing messages, tools, and context.</param>
    /// <param name="frontendTools">The frontend tools (converted from request.Tools).</param>
    /// <param name="session">
    /// The MAF session to run within, or <see langword="null"/> to start a fresh session (the previous
    /// behaviour).
    /// </param>
    /// <param name="pendingApprovalCalls">
    /// Optional map of <c>callId → original approval request</c> reconstructed from persisted history,
    /// used to correlate human-approval resume entries after a reload (when the original call is not in
    /// the client-supplied messages). The full <see cref="ToolApprovalRequestContent"/> is kept (not just
    /// its wrapped tool call) so the resume path can build its response via
    /// <c>request.CreateResponse(approved)</c>, carrying forward FICC's own
    /// <see cref="ToolApprovalRequestContent.RequestId"/> — required for
    /// <c>Microsoft.Agents.AI</c>'s <c>ApprovalResponseBindingChatClient</c> to recognize the response as
    /// tied to a request it actually surfaced. Null for the contextual Copilot.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of AG-UI events.</returns>
    /// <remarks>
    /// Default interface method: implementations that predate session binding inherit this default,
    /// which ignores <paramref name="session"/>/<paramref name="pendingApprovalCalls"/> and delegates to
    /// the core overload.
    /// </remarks>
    IAsyncEnumerable<IAGUIEvent> StreamAgentAsync(
        AIAgent agent,
        AGUIRunRequest request,
        IEnumerable<AITool>? frontendTools,
        AgentSession? session,
        IReadOnlyDictionary<string, ToolApprovalRequestContent>? pendingApprovalCalls = null,
        CancellationToken cancellationToken = default)
        => StreamAgentAsync(agent, request, frontendTools, cancellationToken);
}

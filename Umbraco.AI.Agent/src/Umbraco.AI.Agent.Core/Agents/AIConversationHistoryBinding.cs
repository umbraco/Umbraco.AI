using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Binds an agent execution to a server-side conversation history store. Supplied on
/// <see cref="AIAgentExecutionOptions.ConversationHistory"/> by surfaces that persist their
/// conversations (e.g. Copilot Workspace); left null by every other caller, which keeps the run
/// byte-for-byte unchanged.
/// </summary>
/// <remarks>
/// The Agent layer stays ignorant of any specific persistence product: it attaches the MAF
/// <paramref name="Provider"/> when creating the agent, and — because binding a conversation id into a
/// session is provider-specific — invokes <paramref name="BindSession"/> on the freshly created session
/// rather than calling any concrete provider API itself. The consumer closes <paramref name="BindSession"/>
/// over its concrete provider and the target <paramref name="ConversationId"/>.
/// </remarks>
/// <param name="Provider">
/// The MAF chat-history provider to attach (e.g. a store backed by the durable conversation repository).
/// </param>
/// <param name="ConversationId">
/// The conversation this run belongs to. Surfaced into the runtime context and notifications for
/// telemetry/correlation.
/// </param>
/// <param name="BindSession">
/// Binds the run's session to <paramref name="ConversationId"/>. Invoked once, on the session created
/// for this run, before streaming begins.
/// </param>
public sealed record AIConversationHistoryBinding(
    ChatHistoryProvider Provider,
    Guid ConversationId,
    Action<AgentSession> BindSession)
{
    /// <summary>
    /// Optional resolver that, given a set of approval <c>callId</c>s, returns the original approval
    /// requests recovered from persisted history — used to correlate human-approval resume entries after
    /// a reload, when the original call is no longer in the client-supplied messages (B2). The consumer
    /// closes this over its own conversation store; the Agent layer stays product-agnostic.
    /// </summary>
    /// <remarks>
    /// Returns the full <see cref="ToolApprovalRequestContent"/>, not just its wrapped tool call, so the
    /// resume path can build its response via <c>request.CreateResponse(approved)</c> — carrying forward
    /// FICC's own <see cref="ToolApprovalRequestContent.RequestId"/> rather than guessing it from the
    /// callId. <c>Microsoft.Agents.AI</c>'s <c>ApprovalResponseBindingChatClient</c> matches inbound
    /// responses by that id and silently drops any built with the wrong one.
    /// </remarks>
    public Func<IReadOnlyCollection<string>, CancellationToken, ValueTask<IReadOnlyDictionary<string, ToolApprovalRequestContent>>>? ResolveApprovalToolCalls { get; init; }

    /// <summary>
    /// Optional loader for the conversation's persisted MAF session-state blob (previously captured by
    /// <see cref="SaveSessionState"/>). When set and a blob is available, the agent layer restores the
    /// run's session from it via <c>AIAgent.DeserializeSessionAsync</c> instead of creating a bare one —
    /// required for session-scoped decorators (e.g. tool-approval-response binding) whose state lives on
    /// the session object itself rather than in chat history, and therefore does not survive a fresh
    /// session being created for every request. Returns null when there is nothing to restore.
    /// </summary>
    public Func<CancellationToken, ValueTask<JsonElement?>>? LoadSessionState { get; init; }

    /// <summary>
    /// Optional saver for the run's session state, called after streaming completes (success or
    /// interrupt) so the next request can restore it via <see cref="LoadSessionState"/>. The consumer
    /// closes this over its own conversation store.
    /// </summary>
    public Func<JsonElement, CancellationToken, ValueTask>? SaveSessionState { get; init; }
}

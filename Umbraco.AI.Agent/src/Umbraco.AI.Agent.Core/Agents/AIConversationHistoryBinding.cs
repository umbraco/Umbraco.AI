using Microsoft.Agents.AI;

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
    Action<AgentSession> BindSession);

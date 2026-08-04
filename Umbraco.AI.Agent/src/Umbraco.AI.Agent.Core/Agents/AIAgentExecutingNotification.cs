using Microsoft.Extensions.AI;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Published before an AIAgent is executed (cancelable).
/// </summary>
public sealed class AIAgentExecutingNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentExecutingNotification"/> class.
    /// </summary>
    /// <param name="agent">The agent being executed.</param>
    /// <param name="chatMessages">The chat messages for this execution.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIAgentExecutingNotification(
        AIAgent agent,
        IReadOnlyList<ChatMessage> chatMessages,
        EventMessages messages)
        : base(messages)
    {
        Agent = agent;
        ChatMessages = chatMessages;
    }

    /// <summary>
    /// Gets the agent being executed.
    /// </summary>
    public AIAgent Agent { get; }

    /// <summary>
    /// Gets the chat messages for this execution.
    /// </summary>
    public IReadOnlyList<ChatMessage> ChatMessages { get; }

    /// <summary>
    /// Gets the id of the surface driving this execution (e.g. the Copilot Workspace surface), if any.
    /// Null for executions with no associated surface. Additive; existing consumers are unaffected.
    /// </summary>
    public string? ExecutionSurfaceId { get; init; }

    /// <summary>
    /// Gets the id of the persisted conversation this execution belongs to, if any. Populated for
    /// surfaces with server-side conversation persistence (Copilot Workspace); null otherwise.
    /// </summary>
    public Guid? ConversationId { get; init; }
}

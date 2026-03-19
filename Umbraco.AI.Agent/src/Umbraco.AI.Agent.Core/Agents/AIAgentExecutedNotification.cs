using Microsoft.Extensions.AI;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Published after an AIAgent execution completes (not cancelable).
/// </summary>
public sealed class AIAgentExecutedNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentExecutedNotification"/> class.
    /// </summary>
    /// <param name="agent">The agent that was executed.</param>
    /// <param name="chatMessages">The chat messages for this execution.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="isSuccess">Whether the execution completed successfully.</param>
    /// <param name="messages">Event messages from the execution operation.</param>
    public AIAgentExecutedNotification(
        AIAgent agent,
        IReadOnlyList<ChatMessage> chatMessages,
        TimeSpan duration,
        bool isSuccess,
        EventMessages messages)
    {
        Agent = agent;
        ChatMessages = chatMessages;
        Duration = duration;
        IsSuccess = isSuccess;
        Messages = messages;
    }

    /// <summary>
    /// Gets the agent that was executed.
    /// </summary>
    public AIAgent Agent { get; }

    /// <summary>
    /// Gets the chat messages for this execution.
    /// </summary>
    public IReadOnlyList<ChatMessage> ChatMessages { get; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets whether the execution completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}

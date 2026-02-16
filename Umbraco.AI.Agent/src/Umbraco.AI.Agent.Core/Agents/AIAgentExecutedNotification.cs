using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.Tools;
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
    /// <param name="request">The execution run request.</param>
    /// <param name="frontendTools">The frontend tools provided for this execution.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="isSuccess">Whether the execution completed successfully.</param>
    /// <param name="error">The error that occurred during execution, if any.</param>
    /// <param name="messages">Event messages from the execution operation.</param>
    public AIAgentExecutedNotification(
        AIAgent agent,
        AGUIRunRequest request,
        IEnumerable<AIFrontendTool>? frontendTools,
        TimeSpan duration,
        bool isSuccess,
        Exception? error,
        EventMessages messages)
    {
        Agent = agent;
        Request = request;
        FrontendTools = frontendTools;
        Duration = duration;
        IsSuccess = isSuccess;
        Error = error;
        Messages = messages;
    }

    /// <summary>
    /// Gets the agent that was executed.
    /// </summary>
    public AIAgent Agent { get; }

    /// <summary>
    /// Gets the execution run request.
    /// </summary>
    public AGUIRunRequest Request { get; }

    /// <summary>
    /// Gets the frontend tools provided for this execution.
    /// </summary>
    public IEnumerable<AIFrontendTool>? FrontendTools { get; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets whether the execution completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error that occurred during execution, if any.
    /// </summary>
    public Exception? Error { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}

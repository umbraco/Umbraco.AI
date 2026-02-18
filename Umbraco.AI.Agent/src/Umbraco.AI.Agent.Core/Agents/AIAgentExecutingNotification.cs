using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Core.Tools;
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
    /// <param name="request">The execution run request.</param>
    /// <param name="frontendTools">The frontend tools provided for this execution.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIAgentExecutingNotification(
        AIAgent agent,
        AGUIRunRequest request,
        IEnumerable<AIFrontendTool>? frontendTools,
        EventMessages messages)
        : base(messages)
    {
        Agent = agent;
        Request = request;
        FrontendTools = frontendTools;
    }

    /// <summary>
    /// Gets the agent being executed.
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
}

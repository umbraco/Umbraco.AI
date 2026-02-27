using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Published after an AIAgent is deleted (not cancelable).
/// </summary>
public sealed class AIAgentDeletedNotification : AIEntityDeletedNotification<AIAgent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the agent that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AIAgentDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    { }
}

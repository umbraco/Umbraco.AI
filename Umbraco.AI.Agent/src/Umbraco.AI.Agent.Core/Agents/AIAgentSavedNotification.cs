using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Published after an AIAgent is saved (not cancelable).
/// </summary>
public sealed class AIAgentSavedNotification : AIEntitySavedNotification<AIAgent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The agent that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AIAgentSavedNotification(AIAgent entity, EventMessages messages)
        : base(entity, messages)
    { }
}

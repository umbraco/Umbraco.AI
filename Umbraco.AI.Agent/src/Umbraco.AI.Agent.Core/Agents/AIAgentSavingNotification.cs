using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Published before an AIAgent is saved (cancelable).
/// </summary>
public sealed class AIAgentSavingNotification : AIEntitySavingNotification<AIAgent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The agent being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIAgentSavingNotification(AIAgent entity, EventMessages messages)
        : base(entity, messages)
    { }
}

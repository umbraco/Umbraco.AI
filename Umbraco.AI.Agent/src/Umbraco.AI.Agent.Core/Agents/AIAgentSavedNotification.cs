using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Published after an AIAgent is saved (not cancelable).
/// </summary>
public sealed class AIAgentSavedNotification : AIEntitySavedNotification<AIAgent>
{
    public AIAgentSavedNotification(AIAgent entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Published after an AIAgent is deleted (not cancelable).
/// </summary>
public sealed class AIAgentDeletedNotification : AIEntityDeletedNotification<AIAgent>
{
    public AIAgentDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

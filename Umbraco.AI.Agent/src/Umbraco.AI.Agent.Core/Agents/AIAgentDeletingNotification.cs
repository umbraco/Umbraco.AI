using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Published before an AIAgent is deleted (cancelable).
/// </summary>
public sealed class AIAgentDeletingNotification : AIEntityDeletingNotification<AIAgent>
{
    public AIAgentDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

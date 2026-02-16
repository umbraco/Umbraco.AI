using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Published before an AIAgent is saved (cancelable).
/// </summary>
public sealed class AIAgentSavingNotification : AIEntitySavingNotification<AIAgent>
{
    public AIAgentSavingNotification(AIAgent entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

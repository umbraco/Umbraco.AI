using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Published after an AIConnection is deleted (not cancelable).
/// </summary>
public sealed class AIConnectionDeletedNotification : AIEntityDeletedNotification<AIConnection>
{
    public AIConnectionDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

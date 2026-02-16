using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Published before an AIConnection is deleted (cancelable).
/// </summary>
public sealed class AIConnectionDeletingNotification : AIEntityDeletingNotification<AIConnection>
{
    public AIConnectionDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

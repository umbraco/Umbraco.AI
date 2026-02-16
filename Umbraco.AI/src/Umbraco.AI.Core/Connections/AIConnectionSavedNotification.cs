using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Published after an AIConnection is saved (not cancelable).
/// </summary>
public sealed class AIConnectionSavedNotification : AIEntitySavedNotification<AIConnection>
{
    public AIConnectionSavedNotification(AIConnection entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

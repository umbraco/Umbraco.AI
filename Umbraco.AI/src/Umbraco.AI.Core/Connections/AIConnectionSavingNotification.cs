using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Published before an AIConnection is saved (cancelable).
/// </summary>
public sealed class AIConnectionSavingNotification : AIEntitySavingNotification<AIConnection>
{
    public AIConnectionSavingNotification(AIConnection entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

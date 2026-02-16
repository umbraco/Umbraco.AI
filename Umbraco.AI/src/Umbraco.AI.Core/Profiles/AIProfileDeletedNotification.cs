using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Published after an AIProfile is deleted (not cancelable).
/// </summary>
public sealed class AIProfileDeletedNotification : AIEntityDeletedNotification<AIProfile>
{
    public AIProfileDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

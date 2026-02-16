using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Published before an AIProfile is deleted (cancelable).
/// </summary>
public sealed class AIProfileDeletingNotification : AIEntityDeletingNotification<AIProfile>
{
    public AIProfileDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

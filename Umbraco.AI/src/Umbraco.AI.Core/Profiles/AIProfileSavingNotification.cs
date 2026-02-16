using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Published before an AIProfile is saved (cancelable).
/// </summary>
public sealed class AIProfileSavingNotification : AIEntitySavingNotification<AIProfile>
{
    public AIProfileSavingNotification(AIProfile entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

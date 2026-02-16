using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Published after an AIProfile is saved (not cancelable).
/// </summary>
public sealed class AIProfileSavedNotification : AIEntitySavedNotification<AIProfile>
{
    public AIProfileSavedNotification(AIProfile entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

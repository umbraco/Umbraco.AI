using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Published after an AIContext is saved (not cancelable).
/// </summary>
public sealed class AIContextSavedNotification : AIEntitySavedNotification<AIContext>
{
    public AIContextSavedNotification(AIContext entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

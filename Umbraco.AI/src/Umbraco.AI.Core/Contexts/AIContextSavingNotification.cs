using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Published before an AIContext is saved (cancelable).
/// </summary>
public sealed class AIContextSavingNotification : AIEntitySavingNotification<AIContext>
{
    public AIContextSavingNotification(AIContext entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Published before an AIContext is deleted (cancelable).
/// </summary>
public sealed class AIContextDeletingNotification : AIEntityDeletingNotification<AIContext>
{
    public AIContextDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

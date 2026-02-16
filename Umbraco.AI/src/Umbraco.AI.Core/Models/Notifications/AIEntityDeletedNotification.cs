using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Models.Notifications;

/// <summary>
/// Base class for all entity deleted notifications (not cancelable, published after delete).
/// </summary>
/// <typeparam name="T">The entity type that was deleted.</typeparam>
public abstract class AIEntityDeletedNotification<T> : StatefulNotification
{
    protected AIEntityDeletedNotification(Guid entityId, EventMessages messages)
        : base(messages)
    {
        EntityId = entityId;
    }

    /// <summary>
    /// Gets the ID of the entity that was deleted.
    /// </summary>
    public Guid EntityId { get; }
}

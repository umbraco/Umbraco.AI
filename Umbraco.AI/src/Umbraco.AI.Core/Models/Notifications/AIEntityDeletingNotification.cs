using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Models.Notifications;

/// <summary>
/// Base class for all entity deleting notifications (cancelable, published before delete).
/// </summary>
/// <typeparam name="T">The entity type being deleted.</typeparam>
public abstract class AIEntityDeletingNotification<T> : CancelableNotification
{
    protected AIEntityDeletingNotification(Guid entityId, EventMessages messages)
        : base(messages)
    {
        EntityId = entityId;
    }

    /// <summary>
    /// Gets the ID of the entity being deleted.
    /// </summary>
    public Guid EntityId { get; }
}

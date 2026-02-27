using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Models.Notifications;

/// <summary>
/// Base class for all entity saving notifications (cancelable, published before save).
/// </summary>
/// <typeparam name="T">The entity type being saved.</typeparam>
public abstract class AIEntitySavingNotification<T> : CancelableNotification
{
    protected AIEntitySavingNotification(T entity, EventMessages messages)
        : base(messages)
    {
        Entity = entity;
    }

    /// <summary>
    /// Gets the entity being saved.
    /// </summary>
    public T Entity { get; }
}

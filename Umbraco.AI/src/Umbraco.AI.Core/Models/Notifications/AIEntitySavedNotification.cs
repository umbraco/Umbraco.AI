using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Models.Notifications;

/// <summary>
/// Base class for all entity saved notifications (not cancelable, published after save).
/// </summary>
/// <typeparam name="T">The entity type that was saved.</typeparam>
public abstract class AIEntitySavedNotification<T> : StatefulNotification
{
    protected AIEntitySavedNotification(T entity, EventMessages messages)
        : base(messages)
    {
        Entity = entity;
    }

    /// <summary>
    /// Gets the entity that was saved.
    /// </summary>
    public T Entity { get; }
}

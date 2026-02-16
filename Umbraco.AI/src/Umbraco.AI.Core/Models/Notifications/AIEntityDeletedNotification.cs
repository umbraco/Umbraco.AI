using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Models.Notifications;

/// <summary>
/// Base class for all entity deleted notifications (not cancelable, published after delete).
/// </summary>
/// <typeparam name="T">The entity type that was deleted.</typeparam>
public abstract class AIEntityDeletedNotification<T> : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIEntityDeletedNotification{T}"/> class.
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="messages"></param>
    protected AIEntityDeletedNotification(Guid entityId, EventMessages messages)
    {
        EntityId = entityId;
        Messages = messages;
    }

    /// <summary>
    /// Gets the ID of the entity that was deleted.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}

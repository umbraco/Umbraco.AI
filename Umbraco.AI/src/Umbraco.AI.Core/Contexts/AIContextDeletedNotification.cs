using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Published after an AIContext is deleted (not cancelable).
/// </summary>
public sealed class AIContextDeletedNotification : AIEntityDeletedNotification<AIContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the context that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AIContextDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

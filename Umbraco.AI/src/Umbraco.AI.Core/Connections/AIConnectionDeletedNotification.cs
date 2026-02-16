using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Published after an AIConnection is deleted (not cancelable).
/// </summary>
public sealed class AIConnectionDeletedNotification : AIEntityDeletedNotification<AIConnection>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConnectionDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the connection that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AIConnectionDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

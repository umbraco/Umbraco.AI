using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Published before an AIConnection is deleted (cancelable).
/// </summary>
public sealed class AIConnectionDeletingNotification : AIEntityDeletingNotification<AIConnection>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConnectionDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the connection being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIConnectionDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

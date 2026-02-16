using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Published after an AIConnection is saved (not cancelable).
/// </summary>
public sealed class AIConnectionSavedNotification : AIEntitySavedNotification<AIConnection>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConnectionSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The connection that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AIConnectionSavedNotification(AIConnection entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

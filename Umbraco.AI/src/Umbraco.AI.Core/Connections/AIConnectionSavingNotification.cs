using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Published before an AIConnection is saved (cancelable).
/// </summary>
public sealed class AIConnectionSavingNotification : AIEntitySavingNotification<AIConnection>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConnectionSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The connection being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIConnectionSavingNotification(AIConnection entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

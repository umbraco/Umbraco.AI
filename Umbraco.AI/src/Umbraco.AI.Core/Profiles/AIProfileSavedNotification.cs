using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Published after an AIProfile is saved (not cancelable).
/// </summary>
public sealed class AIProfileSavedNotification : AIEntitySavedNotification<AIProfile>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The profile that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AIProfileSavedNotification(AIProfile entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

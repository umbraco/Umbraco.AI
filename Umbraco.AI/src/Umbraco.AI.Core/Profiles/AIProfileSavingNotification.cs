using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Published before an AIProfile is saved (cancelable).
/// </summary>
public sealed class AIProfileSavingNotification : AIEntitySavingNotification<AIProfile>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The profile being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIProfileSavingNotification(AIProfile entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

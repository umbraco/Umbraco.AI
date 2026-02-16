using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Published after an AIProfile is deleted (not cancelable).
/// </summary>
public sealed class AIProfileDeletedNotification : AIEntityDeletedNotification<AIProfile>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the profile that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AIProfileDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

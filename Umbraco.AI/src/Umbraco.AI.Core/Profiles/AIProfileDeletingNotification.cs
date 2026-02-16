using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Published before an AIProfile is deleted (cancelable).
/// </summary>
public sealed class AIProfileDeletingNotification : AIEntityDeletingNotification<AIProfile>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the profile being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIProfileDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

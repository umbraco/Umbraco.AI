using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Conversations.Core.Projects;

/// <summary>
/// Published before an <see cref="AIProject"/> is deleted (cancelable). Handlers cancel the delete by
/// setting <see cref="Umbraco.Cms.Core.Notifications.CancelableNotification.Cancel"/> and adding a reason
/// to <see cref="Umbraco.Cms.Core.Notifications.CancelableNotification.Messages"/>.
/// </summary>
public sealed class AIProjectDeletingNotification : AIEntityDeletingNotification<AIProject>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProjectDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the project being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIProjectDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

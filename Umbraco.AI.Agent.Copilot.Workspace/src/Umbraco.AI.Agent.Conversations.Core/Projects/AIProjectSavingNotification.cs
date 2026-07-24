using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Conversations.Core.Projects;

/// <summary>
/// Published before an <see cref="AIProject"/> is saved (created or updated). Cancelable — a handler can
/// veto the save by setting <see cref="Umbraco.Cms.Core.Notifications.CancelableNotification.Cancel"/>.
/// </summary>
public sealed class AIProjectSavingNotification : AIEntitySavingNotification<AIProject>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProjectSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The project being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIProjectSavingNotification(AIProject entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

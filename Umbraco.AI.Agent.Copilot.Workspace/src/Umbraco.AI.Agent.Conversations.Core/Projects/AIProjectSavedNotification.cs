using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Conversations.Core.Projects;

/// <summary>
/// Published after an <see cref="AIProject"/> has been saved (created or updated). Not cancelable.
/// </summary>
public sealed class AIProjectSavedNotification : AIEntitySavedNotification<AIProject>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProjectSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The project that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AIProjectSavedNotification(AIProject entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

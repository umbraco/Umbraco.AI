using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Conversations.Core.Projects;

/// <summary>
/// Published after an <see cref="AIProject"/> has been deleted. Not cancelable.
/// </summary>
public sealed class AIProjectDeletedNotification : AIEntityDeletedNotification<AIProject>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProjectDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the project that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AIProjectDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Published after an AIOrchestration is deleted (not cancelable).
/// </summary>
public sealed class AIOrchestrationDeletedNotification : AIEntityDeletedNotification<AIOrchestration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIOrchestrationDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the orchestration that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AIOrchestrationDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    { }
}

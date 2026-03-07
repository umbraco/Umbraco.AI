using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Published before an AIOrchestration is deleted (cancelable).
/// </summary>
public sealed class AIOrchestrationDeletingNotification : AIEntityDeletingNotification<AIOrchestration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIOrchestrationDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the orchestration being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIOrchestrationDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    { }
}

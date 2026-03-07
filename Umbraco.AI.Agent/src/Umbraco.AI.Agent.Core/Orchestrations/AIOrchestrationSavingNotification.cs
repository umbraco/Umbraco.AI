using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Published before an AIOrchestration is saved (cancelable).
/// </summary>
public sealed class AIOrchestrationSavingNotification : AIEntitySavingNotification<AIOrchestration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIOrchestrationSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The orchestration being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIOrchestrationSavingNotification(AIOrchestration entity, EventMessages messages)
        : base(entity, messages)
    { }
}

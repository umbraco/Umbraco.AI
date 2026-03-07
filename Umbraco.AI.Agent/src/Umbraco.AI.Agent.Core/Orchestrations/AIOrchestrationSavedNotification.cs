using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Published after an AIOrchestration is saved (not cancelable).
/// </summary>
public sealed class AIOrchestrationSavedNotification : AIEntitySavedNotification<AIOrchestration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIOrchestrationSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The orchestration that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AIOrchestrationSavedNotification(AIOrchestration entity, EventMessages messages)
        : base(entity, messages)
    { }
}

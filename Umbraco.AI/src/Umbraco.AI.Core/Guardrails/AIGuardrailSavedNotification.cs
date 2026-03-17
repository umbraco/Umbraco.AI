using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Published after an AIGuardrail is saved (not cancelable).
/// </summary>
public sealed class AIGuardrailSavedNotification : AIEntitySavedNotification<AIGuardrail>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The guardrail that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AIGuardrailSavedNotification(AIGuardrail entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

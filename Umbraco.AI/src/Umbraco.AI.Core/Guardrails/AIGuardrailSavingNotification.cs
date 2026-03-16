using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Published before an AIGuardrail is saved (cancelable).
/// </summary>
public sealed class AIGuardrailSavingNotification : AIEntitySavingNotification<AIGuardrail>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The guardrail being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIGuardrailSavingNotification(AIGuardrail entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

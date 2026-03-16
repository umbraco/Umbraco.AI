using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Published before an AIGuardrail is deleted (cancelable).
/// </summary>
public sealed class AIGuardrailDeletingNotification : AIEntityDeletingNotification<AIGuardrail>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the guardrail being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIGuardrailDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

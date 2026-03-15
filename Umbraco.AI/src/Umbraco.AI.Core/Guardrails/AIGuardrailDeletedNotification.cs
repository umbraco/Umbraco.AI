using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Published after an AIGuardrail is deleted (not cancelable).
/// </summary>
public sealed class AIGuardrailDeletedNotification : AIEntityDeletedNotification<AIGuardrail>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the guardrail that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AIGuardrailDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

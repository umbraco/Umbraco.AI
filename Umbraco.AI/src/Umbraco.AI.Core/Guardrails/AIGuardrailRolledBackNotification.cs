using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Published after an AIGuardrail has been rolled back to a previous version (not cancelable).
/// </summary>
public sealed class AIGuardrailRolledBackNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailRolledBackNotification"/> class.
    /// </summary>
    /// <param name="guardrail">The rolled back guardrail.</param>
    /// <param name="targetVersion">The version number that was rolled back to.</param>
    /// <param name="messages">Event messages from the rollback operation.</param>
    public AIGuardrailRolledBackNotification(AIGuardrail guardrail, int targetVersion, EventMessages messages)
    {
        Guardrail = guardrail;
        TargetVersion = targetVersion;
        Messages = messages;
    }

    /// <summary>
    /// Gets the rolled back guardrail.
    /// </summary>
    public AIGuardrail Guardrail { get; }

    /// <summary>
    /// Gets the version number that was rolled back to.
    /// </summary>
    public int TargetVersion { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}

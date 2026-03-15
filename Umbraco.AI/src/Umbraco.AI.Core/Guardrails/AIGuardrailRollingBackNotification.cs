using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Published before an AIGuardrail is rolled back to a previous version (cancelable).
/// </summary>
public sealed class AIGuardrailRollingBackNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailRollingBackNotification"/> class.
    /// </summary>
    /// <param name="guardrailId">The ID of the guardrail being rolled back.</param>
    /// <param name="targetVersion">The version number to roll back to.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIGuardrailRollingBackNotification(Guid guardrailId, int targetVersion, EventMessages messages)
        : base(messages)
    {
        GuardrailId = guardrailId;
        TargetVersion = targetVersion;
    }

    /// <summary>
    /// Gets the ID of the guardrail being rolled back.
    /// </summary>
    public Guid GuardrailId { get; }

    /// <summary>
    /// Gets the target version number to roll back to.
    /// </summary>
    public int TargetVersion { get; }
}

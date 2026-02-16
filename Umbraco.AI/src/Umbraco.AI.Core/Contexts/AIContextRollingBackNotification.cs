using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Published before an AIContext is rolled back to a previous version (cancelable).
/// </summary>
public sealed class AIContextRollingBackNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextRollingBackNotification"/> class.
    /// </summary>
    /// <param name="contextId">The ID of the context being rolled back.</param>
    /// <param name="targetVersion">The version number to roll back to.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIContextRollingBackNotification(Guid contextId, int targetVersion, EventMessages messages)
        : base(messages)
    {
        ContextId = contextId;
        TargetVersion = targetVersion;
    }

    /// <summary>
    /// Gets the ID of the context being rolled back.
    /// </summary>
    public Guid ContextId { get; }

    /// <summary>
    /// Gets the target version number to roll back to.
    /// </summary>
    public int TargetVersion { get; }
}

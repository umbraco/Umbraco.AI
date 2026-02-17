using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Published before an AIConnection is rolled back to a previous version (cancelable).
/// </summary>
public sealed class AIConnectionRollingBackNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConnectionRollingBackNotification"/> class.
    /// </summary>
    /// <param name="connectionId">The ID of the connection being rolled back.</param>
    /// <param name="targetVersion">The version number to roll back to.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIConnectionRollingBackNotification(Guid connectionId, int targetVersion, EventMessages messages)
        : base(messages)
    {
        ConnectionId = connectionId;
        TargetVersion = targetVersion;
    }

    /// <summary>
    /// Gets the ID of the connection being rolled back.
    /// </summary>
    public Guid ConnectionId { get; }

    /// <summary>
    /// Gets the target version number to roll back to.
    /// </summary>
    public int TargetVersion { get; }
}

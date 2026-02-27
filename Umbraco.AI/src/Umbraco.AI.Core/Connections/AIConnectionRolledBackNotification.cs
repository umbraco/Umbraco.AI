using Umbraco.AI.Core.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Published after an AIConnection has been rolled back to a previous version (not cancelable).
/// </summary>
public sealed class AIConnectionRolledBackNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIConnectionRolledBackNotification"/> class.
    /// </summary>
    /// <param name="connection">The rolled back connection.</param>
    /// <param name="targetVersion">The version number that was rolled back to.</param>
    /// <param name="messages">Event messages from the rollback operation.</param>
    public AIConnectionRolledBackNotification(AIConnection connection, int targetVersion, EventMessages messages)
    {
        Connection = connection;
        TargetVersion = targetVersion;
        Messages = messages;
    }

    /// <summary>
    /// Gets the rolled back connection.
    /// </summary>
    public AIConnection Connection { get; }

    /// <summary>
    /// Gets the version number that was rolled back to.
    /// </summary>
    public int TargetVersion { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}

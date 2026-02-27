using Umbraco.AI.Core.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Published after an AIProfile has been rolled back to a previous version (not cancelable).
/// </summary>
public sealed class AIProfileRolledBackNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileRolledBackNotification"/> class.
    /// </summary>
    /// <param name="profile">The rolled back profile.</param>
    /// <param name="targetVersion">The version number that was rolled back to.</param>
    /// <param name="messages">Event messages from the rollback operation.</param>
    public AIProfileRolledBackNotification(AIProfile profile, int targetVersion, EventMessages messages)
    {
        Profile = profile;
        TargetVersion = targetVersion;
        Messages = messages;
    }

    /// <summary>
    /// Gets the rolled back profile.
    /// </summary>
    public AIProfile Profile { get; }

    /// <summary>
    /// Gets the version number that was rolled back to.
    /// </summary>
    public int TargetVersion { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}

using Umbraco.AI.Core.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Published after an AIContext has been rolled back to a previous version (not cancelable).
/// </summary>
public sealed class AIContextRolledBackNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextRolledBackNotification"/> class.
    /// </summary>
    /// <param name="context">The rolled back context.</param>
    /// <param name="targetVersion">The version number that was rolled back to.</param>
    /// <param name="messages">Event messages from the rollback operation.</param>
    public AIContextRolledBackNotification(AIContext context, int targetVersion, EventMessages messages)
    {
        Context = context;
        TargetVersion = targetVersion;
        Messages = messages;
    }

    /// <summary>
    /// Gets the rolled back context.
    /// </summary>
    public AIContext Context { get; }

    /// <summary>
    /// Gets the version number that was rolled back to.
    /// </summary>
    public int TargetVersion { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}

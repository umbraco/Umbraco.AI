using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Published after an AITest has been rolled back to a previous version (not cancelable).
/// </summary>
public sealed class AITestRolledBackNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AITestRolledBackNotification"/> class.
    /// </summary>
    /// <param name="testId">The ID of the test that was rolled back.</param>
    /// <param name="targetVersion">The version number that was rolled back to.</param>
    /// <param name="messages">Event messages from the rollback operation.</param>
    public AITestRolledBackNotification(Guid testId, int targetVersion, EventMessages messages)
    {
        TestId = testId;
        TargetVersion = targetVersion;
        Messages = messages;
    }

    /// <summary>
    /// Gets the ID of the test that was rolled back.
    /// </summary>
    public Guid TestId { get; }

    /// <summary>
    /// Gets the version number that was rolled back to.
    /// </summary>
    public int TargetVersion { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}

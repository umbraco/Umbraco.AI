using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Published before an AITest is rolled back to a previous version (cancelable).
/// </summary>
public sealed class AITestRollingBackNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AITestRollingBackNotification"/> class.
    /// </summary>
    /// <param name="testId">The ID of the test being rolled back.</param>
    /// <param name="targetVersion">The version number to roll back to.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AITestRollingBackNotification(Guid testId, int targetVersion, EventMessages messages)
        : base(messages)
    {
        TestId = testId;
        TargetVersion = targetVersion;
    }

    /// <summary>
    /// Gets the ID of the test being rolled back.
    /// </summary>
    public Guid TestId { get; }

    /// <summary>
    /// Gets the target version number to roll back to.
    /// </summary>
    public int TargetVersion { get; }
}

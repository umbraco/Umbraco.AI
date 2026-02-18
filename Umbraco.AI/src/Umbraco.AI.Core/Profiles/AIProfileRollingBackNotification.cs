using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Published before an AIProfile is rolled back to a previous version (cancelable).
/// </summary>
public sealed class AIProfileRollingBackNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileRollingBackNotification"/> class.
    /// </summary>
    /// <param name="profileId">The ID of the profile being rolled back.</param>
    /// <param name="targetVersion">The version number to roll back to.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIProfileRollingBackNotification(Guid profileId, int targetVersion, EventMessages messages)
        : base(messages)
    {
        ProfileId = profileId;
        TargetVersion = targetVersion;
    }

    /// <summary>
    /// Gets the ID of the profile being rolled back.
    /// </summary>
    public Guid ProfileId { get; }

    /// <summary>
    /// Gets the target version number to roll back to.
    /// </summary>
    public int TargetVersion { get; }
}

using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Settings;

/// <summary>
/// Notification published when AI settings are deleted.
/// </summary>
public sealed class AISettingsDeletedNotification : AIEntityDeletedNotification<AISettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AISettingsDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the settings entity that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AISettingsDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

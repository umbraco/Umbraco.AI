using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Settings;

/// <summary>
/// Notification published when AI settings are saved.
/// </summary>
public sealed class AISettingsSavedNotification : AIEntitySavedNotification<AISettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AISettingsSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The settings entity that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AISettingsSavedNotification(AISettings entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

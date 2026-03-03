using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Settings;

/// <summary>
/// Notification published before AI settings are saved (cancelable).
/// </summary>
public sealed class AISettingsSavingNotification : AIEntitySavingNotification<AISettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AISettingsSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The settings entity being saved.</param>
    /// <param name="messages">Event messages for the save operation.</param>
    public AISettingsSavingNotification(AISettings entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}

using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Settings;

/// <summary>
/// Notification published before AI settings are deleted (cancelable).
/// </summary>
public sealed class AISettingsDeletingNotification : AIEntityDeletingNotification<AISettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AISettingsDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the settings entity being deleted.</param>
    /// <param name="messages">Event messages for the delete operation.</param>
    public AISettingsDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}

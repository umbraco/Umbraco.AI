using Umbraco.AI.Core.Models.Notifications;

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
    /// <param name="savedEntities">All settings entities that were saved in this operation.</param>
    public AISettingsSavedNotification(AISettings entity, IEnumerable<AISettings> savedEntities)
        : base(entity, savedEntities)
    {
    }
}

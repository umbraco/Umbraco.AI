using Umbraco.AI.Core.Models.Notifications;

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
    /// <param name="deletedEntities">All settings entities that were deleted in this operation.</param>
    public AISettingsDeletedNotification(Guid entityId, IEnumerable<Guid> deletedEntities)
        : base(entityId, deletedEntities)
    {
    }
}

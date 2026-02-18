using Umbraco.AI.Core.Settings;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

/// <summary>
/// Handles AI settings deleted notifications and removes deployment artifacts.
/// Note: Settings is a singleton and deletion is unlikely, but this handler
/// is included for completeness and consistency.
/// </summary>
internal sealed class AISettingsDeletedDeployRefresherNotificationAsyncHandler
    : UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase<AISettings, AISettingsDeletedNotification>
{
    public AISettingsDeletedDeployRefresherNotificationAsyncHandler(
        IDiskEntityService diskEntityService,
        ISignatureService signatureService)
        : base(diskEntityService, signatureService, UmbracoAIConstants.UdiEntityType.Settings)
    {
    }
}
